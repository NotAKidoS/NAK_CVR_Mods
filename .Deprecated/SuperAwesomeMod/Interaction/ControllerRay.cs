/*using System;
using System.Collections.Generic;
using ABI.CCK.Components;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Base;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Scripting.Attributes;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.RuntimeDebug;
using ABI.Scripting.CVRSTL.Client;
using HighlightPlus;
using TMPro;
using Unity.XR.OpenVR;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using Button = UnityEngine.UI.Button;
using EventTrigger = UnityEngine.EventSystems.EventTrigger;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace ABI_RC.Core.InteractionSystem
{
    [AvailableToScripting(
        targetModule: TargetModule,
        inheritFrom: typeof(MonoBehaviour), 
        optInMembers: true)]
    public class ControllerRay : MonoBehaviour
    {
        public const string TargetModule = LuaScriptFactory.ModuleNames.CVR;

        private const float MAX_RAYCAST_LENGTH = 100f;
        
        [AvailableToScripting(@readonly: true)]
        public Vector3 RayDirection = new Vector3(0,-0.2f,1f);

        [AvailableToScripting(@readonly: true)]
        public CVRHand hand;

        [AvailableToScripting(@readonly: true)]
        public bool isInteractionRay = true;

        [FormerlySerializedAs("triggerGazeEvents")] public bool triggerHoverEvents = true;

        public bool shouldPhysicalInteraction = false;
        
        public VelocityTracker pickupVelocityTracker;
        
        public LineRenderer lineRenderer;

        [FormerlySerializedAs("_vrActive")] [SerializeField] bool vrActive;

        [AvailableToScripting(@readonly: true)]
        [SerializeField] private Transform hitTransform;
        [SerializeField] float thickness = 0.002f;

        [AvailableToScripting(@readonly: true)]
        [SerializeField] internal Pickupable grabbedObject;

        public LayerMask uiMask;
        public LayerMask generalMask;

        [AvailableToScripting(@readonly: true)]
        public Transform controllerTransform;

        [AvailableToScripting(@readonly: true)]
        public bool enabled;

        [AvailableToScripting(@readonly: true)]
        public bool isDesktopRay;

        [AvailableToScripting(@readonly: true)]
        public bool isHeadRay;

        [AvailableToScripting(@readonly: true)]
        public ControllerRay otherRay;

        public bool uiActive;

        public Material highlightMaterial;

        private float _lastTrigger;
        private float _lastGrip;
        private bool _objectWasHit = false;
        
        private CohtmlControlledView _lastView;
        private Vector2 _lastUiCoords;
        private float _lastHitDistance;

        private GameObject lastTarget;
        private GameObject highlightGameObject;
        private GameObject lastTelepathicGrabTarget;
        private GameObject lastProximityGrabTarget;
        private Slider lastSlider;
        private RectTransform lastSliderRect;
        private EventTrigger lastEventTrigger;
        private ScrollRect lastScrollView;
        private RectTransform lastScrollViewRect;
        private Vector2 scrollStartPositionView;
        private Vector2 scrollStartPositionContent;
        
        private Button lastButton;
        private Dropdown lastDropdown;
        private Toggle lastToggle;
        private InputField lastInputField;
        private TMP_InputField lastTMPInputField;
        [SerializeField]
        private Interactable lastInteractable;

        [SerializeField]
        private Pickupable _telepathicPickupCandidate;
        private bool _telepathicPickupTargeted;
        private float _telepathicPickupTimer = 0f;
        private float _telepathicPickupResetTime = 0.5f;
        private bool _telepathicPickupLocked = false;
        private List<Vector3> _positionMemory = new List<Vector3>();
        private List<float> _timeMemory = new List<float>();

        private bool _enableHighlight;
        private bool _enableSmoothRay;

        public Transform rayDirectionTransform;
        public Transform attachmentPoint;
        public Transform pivotPoint;
        public float attachmentDistance = 0.2f;
        public float currentAttachmentDistance = 0.2f;

        public Transform backupRayFor;
        public GameObject backupCrossHair;

        private bool _enableTelepathicGrab = true;
        private float _telepathicGrabMaxDistance = 50f;
        
        private bool _gripToGrab;
        
        public Vector3 HitPoint => _hit.point;

        private RaycastHit _hit;
        private readonly RaycastHit[] _hits = new RaycastHit[10];
        /// <summary>
        /// Comparer used to sort arrays of RaycastHits, it will result in an array where the first indexes are the closest ones.
        /// </summary>
        private readonly Comparer<RaycastHit> _hitsComparer = Comparer<RaycastHit>.Create((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        #region Proximity Grab Fields

        public const string ProximityGrabEnabled = "ExperimentalProximityGrabEnabled";

        private bool _proximityGrabEnabled;
        public const string ProximityGrabVisualizers = "ExperimentalProximityGrabVisualizers";
        public const string ProximityGrabRadiusScale = "ExperimentalProximityGrabRadiusScale";

        public const float ProximityGrabRadiusScaleDefault = 0.1f;
        private bool _proximityGrabVisualizers;

        private float _proximityDetectionRadiusRelativeValue = ProximityGrabRadiusScaleDefault;
        private float ProximityDetectionRadius => _proximityDetectionRadiusRelativeValue * PlayerSetup.Instance.GetPlaySpaceScale();

        private readonly Collider[] _proximityColliders = new Collider[15];

        #endregion

        //Inputs
        private bool _interactDown = false;
        private Pickupable _proximityColliderClosestPickup;
        private bool _proximityCalculatedThisFrame;
        private bool _interactUp = false;
        private bool _interact = false;
        private bool _gripDown = false;
        private bool _gripUp = false;
        private bool _grip = false;
        private bool _isTryingToPickup = false;
        private bool _drop = false;

        private bool _hitUIInternal = false;

        private void Start()
        {
            vrActive = MetaPort.Instance.isUsingVr;
            
            #if !PLATFORM_ANDROID
            bool isOpenVR = XRGeneralSettings.Instance.Manager.activeLoader is OpenVRLoader;
            if (!isOpenVR)
            {
                transform.localPosition = new Vector3(0.001f, 0.021f, -0.009f);
                transform.localRotation = new Quaternion(0.50603801f, 0.0288440064f, 0.0147214793f, 0.861903071f);
            }
            #else
            bool isPxr = XRGeneralSettings.Instance.Manager.activeLoader is PXR_Loader;
            if (!isPxr)
            {
                transform.localPosition = new Vector3(0.001f, 0.021f, -0.009f);
                transform.localRotation = new Quaternion(0.50603801f, 0.0288440064f, 0.0147214793f, 0.861903071f);
            }
            else
            {
                transform.localPosition = new Vector3(0.001f, 0.021f, -0.009f);
                transform.localRotation = new Quaternion(0.30603801f, 0.0288440064f, 0.0147214793f, 0.861903071f);
            }
            #endif
            
            // Fallback 
            if (controllerTransform == null)
                controllerTransform = transform;

            _enableHighlight = MetaPort.Instance.settings.GetSettingsBool("GeneralInteractableHighlight");
            _enableTelepathicGrab = MetaPort.Instance.settings.GetSettingsBool("GeneralTelepathicGrip");
            _gripToGrab = MetaPort.Instance.settings.GetSettingsBool("ControlUseGripToGrab");
            _enableSmoothRay = MetaPort.Instance.settings.GetSettingsBool("ControlSmoothRaycast");
            _proximityGrabEnabled = MetaPort.Instance.settings.GetSettingsBool(ProximityGrabEnabled);
            _proximityGrabVisualizers = MetaPort.Instance.settings.GetSettingsBool(ProximityGrabVisualizers) || MetaPort.Instance.showProximityGrabVisualizers;
            MetaPort.Instance.settings.settingBoolChanged.AddListener(SettingsBoolChanged);

            _proximityDetectionRadiusRelativeValue = MetaPort.Instance.settings.GetSettingsFloat(ProximityGrabRadiusScale);
            MetaPort.Instance.settings.settingFloatChanged.AddListener(SettingFloatChanged);

            // RayDirection object, we smooth to combat jitter hands for the ray, but not pickups
            rayDirectionTransform = new GameObject("RayDirection").transform;
            rayDirectionTransform.SetParent(transform);
            rayDirectionTransform.SetLocalPositionAndRotation(RayDirection * attachmentDistance, Quaternion.identity);
            rayDirectionTransform.localScale = Vector3.one;
            gameObject.BroadcastMessage("RayDirectionTransformUpdated",SendMessageOptions.DontRequireReceiver);
            
            // Attachment point, snaps to the object collider on grab, used for object push/pull
            attachmentPoint = new GameObject("AttachmentPoint").transform;
            attachmentPoint.SetParent(transform);
            attachmentPoint.SetLocalPositionAndRotation(RayDirection * attachmentDistance, Quaternion.identity);
            attachmentPoint.localScale = Vector3.one;
            
            // Pivot point, used purely for object rotation, makes the math easy
            pivotPoint = new GameObject("PivotPoint").transform;
            pivotPoint.SetParent(attachmentPoint);
            pivotPoint.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            pivotPoint.localScale = Vector3.one;

            if (lineRenderer != null)
            {
                lineRenderer.sortingOrder = 10;
            }

            pickupVelocityTracker = pivotPoint.AddComponentIfMissing<VelocityTracker>();
        }

        private void OnDisable()
        {
            if (backupCrossHair) backupCrossHair.SetActive(true);
        }
        
        public void UpdateGrabDistance(float distance = 0.2f)
        {
            Vector3 lossyScale = attachmentPoint.lossyScale;
            Vector3 scale = new(1f/lossyScale.x, 1f/lossyScale.y, 1f/lossyScale.z);
            currentAttachmentDistance = distance;
            scale.Scale(RayDirection);
            attachmentPoint.localPosition = scale * distance;
        }

        public void SetPivotRotation(Quaternion rot)
        {
            pivotPoint.rotation = rot;
        }

        public void ResetPivotRotation()
        {
            pivotPoint.localRotation = Quaternion.identity;
        }

        private void SettingsBoolChanged(string name, bool value)
        {
            if (name == "GeneralInteractableHighlight")
                _enableHighlight = value;
            
            if (name == "GeneralTelepathicGrip")
                _enableTelepathicGrab = value;
            
            if (name == "ControlUseGripToGrab")
                _gripToGrab = value;

            if (name == "ControlSmoothRaycast")
                _enableSmoothRay = value;

            if (name == ProximityGrabEnabled)
                _proximityGrabEnabled = value;

            if (name == ProximityGrabVisualizers)
                _proximityGrabVisualizers = value;
        }

        private void SettingFloatChanged(string settingName, float value)
        {
            if (settingName == ProximityGrabRadiusScale)
                _proximityDetectionRadiusRelativeValue = value;
        }

        public bool IsInteracting()
        {
            bool shouldDisplay = _interact || _grip || _objectWasHit;
            return shouldDisplay;
        }
        
        public void ClearGrabbedObject()
        {
            grabbedObject = null;
        }

        private RaycastHit SphereCast(float offset, float radius, float maxDistance)
        {
            // Might be slower?
            //_proximityColliders[0] = new();
            //Physics.SphereCastNonAlloc

            Physics.SphereCast
                (transform.TransformPoint(RayDirection * offset),
                radius,
                transform.TransformDirection(RayDirection),
                out RaycastHit output,
                maxDistance,
                (1 << CVRLayers.MirrorReflection), // generalMask |= (1 << CVRLayers.MirrorReflection),
                QueryTriggerInteraction.Collide);

            //return _proximityColliders[0];
            return output;
        }

        private void UpdateBackupRay()
        {
            if (backupRayFor != null)
            {
                isInteractionRay = uiActive = backupRayFor.localPosition == Vector3.zero;
                backupCrossHair.SetActive(isInteractionRay);
                if (otherRay != null) otherRay.isInteractionRay = !isInteractionRay;
            }
        }

        private bool CanSelectPlayersAndProps()
        {
            if (ViewManager.Instance.IsAnyMenuOpen)
                return true;
            
            if (isDesktopRay 
                && CVRInputManager.Instance.unlockMouse)
                return true;
            
            return false;
        }
        
        private void UpdateInteractionMask()
        {
            if (CanSelectPlayersAndProps())
            {
                generalMask |= (1 << CVRLayers.PlayerNetwork);
            }
            else
            {
                generalMask &= ~(1 << CVRLayers.PlayerNetwork);
            }
            
            generalMask &= ~(1 << CVRLayers.Water);
        }

        private void ResetPointAtUI()
        {
            if (isInteractionRay
                && !isDesktopRay
                && !isHeadRay)
            {
                if (hand == CVRHand.Left)
                {
                    CVRInputManager.Instance.leftControllerPointingMenu = false;
                    CVRInputManager.Instance.leftControllerPointingCanvas = false;
                }
                else
                {
                    CVRInputManager.Instance.rightControllerPointingMenu = false;
                    CVRInputManager.Instance.rightControllerPointingCanvas = false;
                }
            }
        }

        private void UpdateInputs()
        {
            _interactDown = false;
            _interactUp = false;
            _interact = false;
            _gripDown = false;
            _gripUp = false;
            _grip = false;

            _isTryingToPickup = false;
            _drop = false;
            
            if (isInteractionRay)
            {
                _interactDown = hand == CVRHand.Left ? CVRInputManager.Instance.interactLeftDown : CVRInputManager.Instance.interactRightDown;
                _interactUp = hand == CVRHand.Left ? CVRInputManager.Instance.interactLeftUp : CVRInputManager.Instance.interactRightUp;
                _interact = (hand == CVRHand.Left ? CVRInputManager.Instance.interactLeftValue : CVRInputManager.Instance.interactRightValue) > 0.8f;
                _gripDown = hand == CVRHand.Left ? CVRInputManager.Instance.gripLeftDown : CVRInputManager.Instance.gripRightDown;
                _gripUp = hand == CVRHand.Left ? CVRInputManager.Instance.gripLeftUp : CVRInputManager.Instance.gripRightUp;
                _grip = (hand == CVRHand.Left ? CVRInputManager.Instance.gripLeftValue : CVRInputManager.Instance.gripRightValue) > 0.8f;
                
                if (!MetaPort.Instance.isUsingVr)
                {
                    // Non-VR mode: Use grip for pickup and drop
                    _isTryingToPickup = _gripDown;
                    _drop = _gripUp || CVRInputManager.Instance.drop;
                }
                else if (_gripToGrab)
                {
                    // VR mode with grip-to-grab enabled
                    _isTryingToPickup = _gripDown;
                    _drop = _gripUp;
                }
                else
                {
                    // VR mode with interact button
                    _isTryingToPickup = _interactDown;
                    _drop = _gripDown;
                }
            }
        }

        private void CheckExitPropModes()
        {
            if (_gripDown && PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.None)
            {
                PlayerSetup.Instance.ClearPropToSpawn();
            }
        }

        private void HandleTelepathicGrip()
        {
            if (!_telepathicPickupLocked)
            {
                if (_telepathicPickupCandidate != null && _telepathicPickupTargeted)
                {
                    SetTelepathicGrabTargetHighlight(_telepathicPickupCandidate.gameObject);
                    if (_interactDown) _telepathicPickupLocked = true;
                }
                else if (_telepathicPickupCandidate != null)
                {
                    _telepathicPickupTimer += Time.deltaTime;
                    if (_telepathicPickupTimer >= _telepathicPickupResetTime)
                    {
                        ClearTelepathicGrabTargetHighlight();
                        _telepathicPickupCandidate = null;
                        _telepathicPickupTimer = 0f;
                    }
                }
                else if (_telepathicPickupCandidate == null)
                {
                    _telepathicPickupTimer = 0f;
                }

                _telepathicPickupTargeted = false;
            }
            else
            {
                if (_positionMemory.Count >= 5)
                {
                    _positionMemory.RemoveAt(0);
                    _timeMemory.RemoveAt(0);
                }
                _positionMemory.Add(transform.parent.localPosition);
                _timeMemory.Add(Time.deltaTime);

                if (_positionMemory.Count >= 5)
                {
                    Vector3 velocity = _positionMemory[4] - _positionMemory[0];
                    float time = 0;
                    foreach (float v in _timeMemory)
                    {
                        time += v;
                    }

                    if (MetaPort.Instance.isUsingVr)
                    {
                        if ((velocity * (1 / time)).magnitude >= 0.5f)
                        {
                            if (_telepathicPickupCandidate != null)
                            {
                                _telepathicPickupCandidate.FlingTowardsTarget(transform.position);
                                ClearTelepathicGrabTargetHighlight();
                            }
                            
                            _telepathicPickupCandidate = null;
                            _telepathicPickupTimer = 0f;
                            _telepathicPickupLocked = false;

                            _positionMemory.Clear();
                            _timeMemory.Clear();
                        }
                    }
                    else
                    {
                        if (CVRInputManager.Instance.objectPushPull <= -2)
                        {
                            if (_telepathicPickupCandidate != null)
                            {
                                _telepathicPickupCandidate.FlingTowardsTarget(transform.position);
                                ClearTelepathicGrabTargetHighlight();
                            }

                            _telepathicPickupCandidate = null;
                            _telepathicPickupTimer = 0f;
                            _telepathicPickupLocked = false;

                            _positionMemory.Clear();
                            _timeMemory.Clear();
                        }
                    }
                }

                if (_interactUp)
                {
                    if (_telepathicPickupCandidate != null) ClearTelepathicGrabTargetHighlight();
                    _telepathicPickupCandidate = null;
                    _telepathicPickupTimer = 0f;
                    _telepathicPickupLocked = false;
                    
                    _positionMemory.Clear();
                    _timeMemory.Clear();
                }
            }
        }

        private void HandleGrabbedObjects()
        {
            if (grabbedObject == null) return;
            
            // pickup rotation
            if (grabbedObject.IsObjectRotationAllowed)
            {
                Quaternion rotationX = Quaternion.AngleAxis(-CVRInputManager.Instance.objectRotationValue.x * 20f, transform.up);
                Quaternion rotationY = Quaternion.AngleAxis(CVRInputManager.Instance.objectRotationValue.y * 20f, transform.right);
                pivotPoint.rotation = rotationX * rotationY * pivotPoint.rotation;
            }
            
            // pickup push/pull
            if (grabbedObject.IsObjectPushPullAllowed)
            {
                float newDistance = currentAttachmentDistance + 0.1f 
                    * CVRInputManager.Instance.objectPushPull * PlayerSetup.Instance.GetPlaySpaceScale();
                if (Mathf.Abs(newDistance - currentAttachmentDistance) > float.Epsilon)
                    UpdateGrabDistance(Mathf.Clamp(newDistance, 0f, grabbedObject.MaxPushDistance));
            }
            
            // pickup interaction
            if (_interactDown) grabbedObject.UseDown(new InteractionContext(this));
            if (_interactUp) grabbedObject.UseUp(new InteractionContext(this));
            
            Interactable interactable = grabbedObject.gameObject.GetComponent<Interactable>();

            // interaction
            if (grabbedObject.IsObjectUseAllowed && interactable != null)
            {
                if (_interactDown) interactable.InteractDown(new InteractionContext(this), this);
                if (_interactUp) interactable.InteractUp(new InteractionContext(this), this);
            }

            if (!grabbedObject.IsAutoHold && _drop) DropObject();
            else if (grabbedObject.IsAutoHold && CVRInputManager.Instance.drop) DropObject(true);
            
            DisableLineRenderer();

            DisableProximityGrabHighlight();
            
            if (_telepathicPickupCandidate != null)
            {
                ClearTelepathicGrabTargetHighlight();
                _telepathicPickupCandidate = null;
            }
        }

        private bool HandleMenuUIInteraction()
        {
            //Swap Ray if applicable
            if (_interactDown && _hitUIInternal && !uiActive)
            {
                uiActive = true;
                if (otherRay) otherRay.uiActive = false;
                CVR_MenuManager.Instance.lastInteractedHand = hand;
            }
            
            if (!uiActive) 
                return false;
            
            Ray menuRay = new Ray(rayDirectionTransform.position, rayDirectionTransform.TransformDirection(RayDirection));
            CohtmlControlledView hitView = CohtmlViewInputHandler.Instance.RayToView(menuRay, out float hitDistance, out Vector2 hitCoords);

            if (hitView == null) 
                return false;
            
            float minDistance = 0.15f * PlayerSetup.Instance.GetPlaySpaceScale();
                        
            // physical-like touch, very satisfying
            if (shouldPhysicalInteraction && (hitDistance < minDistance && _lastHitDistance >= minDistance))
                CohtmlViewInputHandler.Instance.DoViewInput(hitView, hitCoords, true, false, 0f);
            else if (shouldPhysicalInteraction && (hitDistance >= minDistance && _lastHitDistance < minDistance))
                CohtmlViewInputHandler.Instance.DoViewInput(hitView, hitCoords, false, true, 0f);
            else
                CohtmlViewInputHandler.Instance.DoViewInput(hitView, hitCoords, _interactDown, _interactUp,
                    CVRInputManager.Instance.scrollValue);
            
            if (_lastView != null && hitView != _lastView)
                CohtmlViewInputHandler.Instance.ReleaseViewInput(_lastView, _lastUiCoords);

            _lastView = hitView;
            _lastUiCoords = hitCoords;
            _lastHitDistance = hitDistance;
            _objectWasHit = true;
                        
            if (isInteractionRay && !isDesktopRay && !isHeadRay)
            {
                if (hand == CVRHand.Left)
                    CVRInputManager.Instance.leftControllerPointingMenu = true;
                else
                    CVRInputManager.Instance.rightControllerPointingMenu = true;
            }

            if (lineRenderer)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(rayDirectionTransform.position));
                lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(rayDirectionTransform.position + rayDirectionTransform.TransformDirection(RayDirection) * hitDistance));
            }

            return true;
        }

        private void DisableLineRenderer()
        {
            if (lineRenderer)
                lineRenderer.enabled = false;

        }

        private void DisableProximityGrabHighlight()
        {
            if (!_proximityCalculatedThisFrame)
                ClearProximityGrabTargetHighlight();
            _proximityCalculatedThisFrame = false;
        }

        private void HandleMenuUIInteractionRelease()
        {
            if (_lastView == null) return;
            if (!_interactUp && uiActive) return;
            CohtmlViewInputHandler.Instance.ReleaseViewInput(_lastView, _lastUiCoords);
            _lastView = null;
        }

        private bool FindTargets(out bool hitUiInternal)
        {
            const int uiInternalMask = 1 << CVRLayers.UIInternal;

            // Transform the point and direction from local to world space
            Vector3 origin = rayDirectionTransform.TransformPoint(RayDirection * -0.15f);
            Vector3 direction = rayDirectionTransform.TransformDirection(RayDirection);

            // Ray Cast to the internal UI layer
            int hitCount = Physics.RaycastNonAlloc(origin, direction, _hits, MAX_RAYCAST_LENGTH, uiInternalMask);

            // Sort hits by distance (closer hits first)
            Array.Sort(_hits, 0, hitCount, _hitsComparer);

            // Iterate through the internal UI layers
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hits[i];
                Collider hitCollider = hit.collider;

                // Ignore Quick Menu from the menu holding ray cast hand
                if (!isDesktopRay && !isHeadRay && CVR_MenuManager.Instance.IsQuickMenuOpen &&
                    hand == CVR_MenuManager.Instance.SelectedQuickMenuHand &&
                    hitCollider == CVR_MenuManager.Instance.quickMenuCollider && 
                    !CVRInputManager.Instance.oneHanded)
                    continue;

                _hit = hit;
                hitTransform = hit.collider.transform;
                hitUiInternal = true;
                return true;
            }

            // General mask except Mirror Reflection and UI Internal (Since we already looked for it)
            int generalLayersMask = generalMask & ~(1 << CVRLayers.MirrorReflection) & ~uiInternalMask;

            // Ray Cast to the general layers
            if (Physics.Raycast(origin, direction, out _hit, MAX_RAYCAST_LENGTH, generalLayersMask))
            {
                hitTransform = _hit.collider.transform;
                hitUiInternal = false;
                return true;
            }
            
            DisableLineRenderer();
            _objectWasHit = false;
            
            if (lastInteractable != null)
            {
                if (triggerHoverEvents) lastInteractable.HoverExit(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                // interactable handles ignoring if we didn't already interact
                lastInteractable.InteractUp(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                lastInteractable = null;
            }

            lastSlider = null;
            
            if (isInteractionRay) CVR_MenuManager.Instance.SetHandTarget("", "", "", Vector3.zero, "", hand);
            if (triggerHoverEvents) CVR_MenuManager.Instance.SetViewTarget("", "", "", Vector3.zero, "");
            
            ClearTargetHighlight();
            hitUiInternal = false;
            return false;
        }

        private Pickupable CheckPickupDirect()
        {
            Pickupable pickup = hitTransform.GetComponent<Pickupable>();
            
            if (pickup == null 
                || !pickup.CanPickup)
                return null;
            
            if (pickup.MaxGrabDistance < _hit.distance) return null;
            
            if (_enableTelepathicGrab && !pickup.IsGrabbedByMe)
            {
                _telepathicPickupCandidate = pickup;
                _telepathicPickupTargeted = true;
            }

            return pickup;
        }

        private PlayerDescriptor HandlePlayerClicked()
        {
            if (PlayerSetup.Instance.GetCurrentPropSelectionMode() 
                != PlayerSetup.PropSelectionMode.None)
                return null;

            PlayerDescriptor descriptor = null;
            if (CanSelectPlayersAndProps() && hitTransform.TryGetComponent(out descriptor))
                if (_interactDown) Users.ShowDetails(descriptor.ownerId);

            return descriptor;
        }

        private Interactable HandleInteractable()
        {
            Interactable interactable = hitTransform.GetComponent<Interactable>();

            // this handles swapping interactables when the raycast hits anything, otherwise
            // FindObjects handles resetting when *nothing* is hit
            // TODO: rework this so lastInteractable cannot be updated if an interaction is in progress
            
            if (interactable != lastInteractable)
            {
                if (triggerHoverEvents)
                {
                    if (lastInteractable) lastInteractable.HoverExit(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                    if (interactable) interactable.HoverEnter(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                }

                // interactable handles ignoring if we didn't already interact
                if (lastInteractable) lastInteractable.InteractUp(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                
                lastInteractable = interactable;
            }
            
            if (interactable == null) return null;
    
            // Ignore interactable interactions if in prop delete or spawn mode
            if (!isInteractionRay || PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.None)
                return interactable;
    
            if (interactable.IsInteractableWithinRange(transform.position))
            {
                if (_interactDown) interactable.InteractDown(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
                if (_interactUp) interactable.InteractUp(new InteractionContext(this, PlayerSetup.PlayerLocalId), this);
            }

            return interactable;
        }

        private CVRSpawnable HandleSpawnableClicked()
        {
            CVRSpawnable spawnable = hitTransform.GetComponentInParent<CVRSpawnable>();
            if (spawnable == null) return null;
            
            PlayerSetup.PropSelectionMode selectionMode = PlayerSetup.Instance.GetCurrentPropSelectionMode();
            switch (selectionMode)
            {
                case PlayerSetup.PropSelectionMode.None:
                {
                    // Click a prop while a menu is open to open the details menu
                    if (_interactDown 
                        && CanSelectPlayersAndProps()
                        && spawnable.TryGetComponent(out CVRAssetInfo assetInfo))
                    {
                        // Open the details menu of the spawnable
                        ViewManager.Instance.GetPropDetails(assetInfo.objectId);
                        ViewManager.Instance.UiStateToggle(true);
                
                        _interactDown = false; // Consume the click
                    }
                    break;
                }
                case PlayerSetup.PropSelectionMode.Delete:
                {
                    // Click a prop while in delete mode to delete it
                    if (_interactDown 
                        && spawnable.ownerId != "SYSTEM" 
                        && spawnable.ownerId != "LocalServer")
                    {
                        spawnable.Delete();
                        _interactDown = false; // Consume the click
                        return null; // Don't return the spawnable, it's been deleted
                    }
                    break;
                }
            }

            // Return normal prop hover
            return spawnable;
        }

        private bool HandleUnityUI()
        {
            if (isDesktopRay && Cursor.lockState != CursorLockMode.Locked) 
                return false; // Unity does this for us
            
            Canvas canvas = hitTransform.GetComponentInParent<Canvas>();
                
            Button button = hitTransform.GetComponent<Button>();
            Toggle toggle = hitTransform.GetComponent<Toggle>();
            Slider slider = hitTransform.GetComponent<Slider>();
            EventTrigger eventTrigger = hitTransform.GetComponent<EventTrigger>();
            InputField inputField = hitTransform.GetComponent<InputField>();
            TMP_InputField tmp_InputField = hitTransform.GetComponent<TMP_InputField>();
            Dropdown dropDown = hitTransform.GetComponent<Dropdown>();
                
            ScrollRect scrollRect = hitTransform.GetComponent<ScrollRect>();

            // Ignore interatable interactions if in prop delete or spawn mode
            if (!isInteractionRay || PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.None) return true;

            //Lock controls when pointing at UI
            if (!isDesktopRay && !isHeadRay)
                if (hand == CVRHand.Left)
                    CVRInputManager.Instance.leftControllerPointingCanvas = canvas != null && canvas.gameObject.layer == CVRLayers.UIInternal;
                else
                    CVRInputManager.Instance.rightControllerPointingCanvas = canvas != null && canvas.gameObject.layer == CVRLayers.UIInternal;

            HandleUnityUIButton(button, ref eventTrigger);

            HandleUnityUIToggle(toggle, ref eventTrigger);

            HandleUnityUIDropdown(dropDown, ref eventTrigger);

            HandleUnityUIInputField(inputField, ref eventTrigger);

            HandleUnityUITMPInputField(tmp_InputField, ref eventTrigger);

            HandleUnityUISlider(slider, ref eventTrigger);

            HandleUnityUIScrollRect(scrollRect, ref eventTrigger);

            HandleUnityUIEventTrigger(eventTrigger);

            if (canvas == null && button == null && toggle == null && slider == null && eventTrigger == null &&
                inputField == null && tmp_InputField == null && dropDown == null && scrollRect == null)
                return false;
            return true;
        }

        private void HandleUnityUIButton(Button button, ref EventTrigger eventTrigger)
        {
            if (button != null)
            {
                if (button != lastButton)
                {
                    button.OnPointerEnter(null);
                    if (lastButton != null) lastButton.OnPointerExit(null);
                    lastButton = button;
                }
                
                if (eventTrigger == null) eventTrigger = button.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    button.onClick.Invoke();
                    if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(button.gameObject))
                        InterfaceAudio.Play(AudioClipField.Click);
                }
            }
            else
            {
                if (lastButton != null) lastButton.OnPointerExit(null);
                lastButton = null;
            }
        }

        private void HandleUnityUIToggle(Toggle toggle, ref EventTrigger eventTrigger)
        {
            if (toggle != null)
            {
                if (toggle != lastToggle)
                {
                    toggle.OnPointerEnter(null);
                    if (lastToggle != null) lastToggle.OnPointerExit(null);
                    lastToggle = toggle;
                }
                
                if (eventTrigger == null) eventTrigger = toggle.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    toggle.isOn = !toggle.isOn;
                    if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(toggle.gameObject))
                        InterfaceAudio.Play(AudioClipField.Click);
                }
            }
            else
            {
                if (lastToggle != null) lastToggle.OnPointerExit(null);
                lastToggle = null;
            }
        }

        private void HandleUnityUIDropdown(Dropdown dropDown, ref EventTrigger eventTrigger)
        {
            if (dropDown != null)
            {
                if (dropDown != lastDropdown)
                {
                    dropDown.OnPointerEnter(null);
                    if (lastDropdown != null) lastDropdown.OnPointerExit(null);
                    lastDropdown = dropDown;
                }

                if (eventTrigger == null) eventTrigger = dropDown.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    if (dropDown.transform.childCount != 3)
                    {
                        dropDown.Hide();
                        if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(dropDown.gameObject))
                            InterfaceAudio.Play(AudioClipField.Click);
                    }
                    else
                    {
                        dropDown.Show();
                        if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(dropDown.gameObject))
                            InterfaceAudio.Play(AudioClipField.Click);
                    }

                    var toggles = dropDown.gameObject.GetComponentsInChildren<Toggle>(true);
                    foreach (var t in toggles)
                    {
                        if (t.GetComponent<BoxCollider>() == null)
                        {
                            BoxCollider col = t.gameObject.AddComponent<BoxCollider>();
                            col.isTrigger = true;
                            var rectTransform = t.gameObject.GetComponent<RectTransform>();
                            col.size = new Vector3(
                                Mathf.Max(rectTransform.sizeDelta.x, rectTransform.rect.width),
                                rectTransform.sizeDelta.y, 0.1f);
                            col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                                col.size.y * (0.5f - rectTransform.pivot.y), 0f);
                        }
                    }
                }
            }
            else
            {
                if (lastDropdown != null) lastDropdown.OnPointerExit(null);
                lastDropdown = null;
            }
        }

        private void HandleUnityUIInputField(InputField inputField, ref EventTrigger eventTrigger)
        {
            if (inputField != null)
            {
                if (inputField != lastInputField)
                {
                    inputField.OnPointerEnter(null);
                    if (lastInputField != null) lastInputField.OnPointerExit(null);
                    lastInputField = inputField;
                }
                
                if (eventTrigger == null) eventTrigger = inputField.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    inputField.Select();
                    inputField.ActivateInputField();
                    ViewManager.Instance.openMenuKeyboard(inputField);
                }
            }
            else
            {
                if (lastInputField != null) lastInputField.OnPointerExit(null);
                lastInputField = null;
            }
        }

        private void HandleUnityUITMPInputField(TMP_InputField tmp_InputField, ref EventTrigger eventTrigger)
        {
            if (tmp_InputField != null)
            {
                if (tmp_InputField != lastTMPInputField)
                {
                    tmp_InputField.OnPointerEnter(null);
                    if (lastTMPInputField != null) lastTMPInputField.OnPointerExit(null);
                    lastTMPInputField = tmp_InputField;
                }
                
                if (eventTrigger == null) eventTrigger = tmp_InputField.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    tmp_InputField.Select();
                    tmp_InputField.ActivateInputField();
                    ViewManager.Instance.openMenuKeyboard(tmp_InputField);
                }
            }
            else
            {
                if (lastTMPInputField != null) lastTMPInputField.OnPointerExit(null);
                lastTMPInputField = null;
            }
        }

        private void HandleUnityUISlider(Slider slider, ref EventTrigger eventTrigger)
        {
            if (slider != null)
            {
                if (eventTrigger == null) eventTrigger = slider.GetComponentInParent<EventTrigger>();
                
                if (_interactDown)
                {
                    lastSlider = slider;
                    lastSliderRect = slider.GetComponent<RectTransform>();
                    SetSliderValueFromRay(slider, _hit, lastSliderRect);
                }
                
                if (_interactUp)
                {
                    if (slider.gameObject.TryGetComponent(out SliderDragHelper sliderDragHelper))
                        sliderDragHelper.Invoke(slider.value);
                }
            }
        }

        private void HandleUnityUIScrollRect(ScrollRect scrollRect, ref EventTrigger eventTrigger)
        {

            // If it's not a scroll rect itself, search for a parent scroll rect
            bool isParentScrollRect = scrollRect == null;
            if (isParentScrollRect)
            {
                scrollRect = hitTransform.GetComponentInParent<ScrollRect>();
                // Ignore if not found
                if (scrollRect == null) return;
            }

            // If directly targeting the scroll rect and there isn't one mark it as the event trigger
            if (!isParentScrollRect && eventTrigger == null)
                eventTrigger = scrollRect.GetComponentInParent<EventTrigger>();

            // Handle the hold and drag of the scroll rect to scroll
            if (_interactDown)
            {
                lastScrollView = scrollRect;
                scrollStartPositionView = GetScreenPositionFromRaycastHit(_hit, scrollRect.viewport);
                scrollStartPositionContent = scrollRect.content.anchoredPosition;
                SetScrollViewValueFromRay(scrollRect, _hit);
            }

            // Handle the scroll input changes
            if (!Mathf.Approximately(CVRInputManager.Instance.scrollValue, 0f))
            {
                Vector2 anchoredPosition = scrollRect.content.anchoredPosition;

                if (scrollRect.vertical)
                {
                    anchoredPosition.y -= CVRInputManager.Instance.scrollValue * 1000f;
                }
                else if (scrollRect.horizontal)
                {
                    anchoredPosition.x -= CVRInputManager.Instance.scrollValue * 1000f;
                }

                scrollRect.content.anchoredPosition = anchoredPosition;
            }

            // Ensure the scroll rect stays within their scrolling limits
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
        }

        private void HandleUnityUIEventTrigger(EventTrigger eventTrigger)
        {
            if (eventTrigger != null)
            {
                if (eventTrigger != lastEventTrigger)
                {
                    if (lastEventTrigger != null)
                    {
                        foreach (var trigger in lastEventTrigger.triggers)
                        {
                            if (trigger.eventID == EventTriggerType.PointerExit)
                            {
                                trigger.callback.Invoke(null);
                            }
                        }
                    }

                    lastEventTrigger = eventTrigger;
                    
                    foreach (var trigger in eventTrigger.triggers)
                    {
                        if (trigger.eventID == EventTriggerType.PointerEnter)
                        {
                            if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(eventTrigger.gameObject)) InterfaceAudio.Play(AudioClipField.Hover);
                            trigger.callback.Invoke(null);
                        }
                    }
                }
                
                if (_interactDown)
                {
                    foreach (var trigger in eventTrigger.triggers)
                    {
                        if (trigger.eventID == EventTriggerType.PointerDown ||
                            trigger.eventID == EventTriggerType.PointerClick)
                        {
                            trigger.callback.Invoke(null);
                            if (CVRWorld.Instance.uiHighlightSoundObjects.Contains(eventTrigger.gameObject))
                                InterfaceAudio.Play(AudioClipField.Click);
                        }
                    }
                }
                
                if (_interactUp)
                {
                    foreach (var trigger in eventTrigger.triggers)
                    {
                        if (trigger.eventID == EventTriggerType.PointerUp)
                        {
                            trigger.callback.Invoke(null);
                        }
                    }
                }
                
                
            }
            if ((eventTrigger == null && lastEventTrigger != null) || (eventTrigger != null && lastEventTrigger != eventTrigger))
            {
                foreach (var trigger in lastEventTrigger.triggers)
                {
                    if (trigger.eventID == EventTriggerType.PointerExit)
                    {
                        trigger.callback.Invoke(null);
                    }
                }

                lastEventTrigger = null;
            }
            else if (eventTrigger != lastEventTrigger)
            {
                foreach (var trigger in lastEventTrigger.triggers)
                {
                    if (trigger.eventID == EventTriggerType.PointerExit)
                    {
                        trigger.callback.Invoke(null);
                    }
                }

                lastEventTrigger = eventTrigger;
            }
        }

        private void HandleIndirectUnityUI()
        {
            if (lastSlider != null)
            {
                if (_interact)
                {
                    SetSliderValueFromRay(lastSlider, _hit, lastSliderRect);
                }
            }
                
            if (lastScrollView != null)
            {
                if (_interact)
                {
                    SetScrollViewValueFromRay(lastScrollView, _hit);
                }
            }
            
            if (_interactUp)
            {
                lastSlider = null;
                lastScrollView = null;
            }
        }

        private void DisplayRayHighlight(bool targetHasComponent, PlayerDescriptor player, Interactable interactable, CVRSpawnable spawnable, Pickupable pickup)
        {
            var targetRay = (interactable && interactable.IsInteractable && interactable.IsInteractableWithinRange(transform.position)) ||
                                (player && CanSelectPlayersAndProps()) ||
                                targetHasComponent || pickup ||
                                (spawnable && PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.None);
            
            if (targetRay && lineRenderer)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(rayDirectionTransform.position));
                lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(_hit.point));
            }
            else if(lineRenderer)
            {
                lineRenderer.enabled = false;
            }
            
            if (targetRay && !_objectWasHit)
            {
                CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 0.1f, hand);
            }

            _objectWasHit = targetRay;
            
            if (lineRenderer && PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.None)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(rayDirectionTransform.position));
                lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(_hit.point));
            }
        }

        private void DisplayAuraHighlight(Pickupable pickup, bool ignorePickup, PlayerDescriptor player, Interactable interactable, CVRSpawnable spawnable)
        {
            GameObject targetHighlightObject = null;

            // Check if the hit object is highlightable, and what root object to highlight from
            if (hitTransform == null)
            {
                targetHighlightObject = null;
            }
            else if (interactable && interactable.IsInteractable 
                             && interactable.IsInteractableWithinRange(transform.position))
            {
                targetHighlightObject = interactable.gameObject;
            }
            else if (pickup && !ignorePickup)
            {
                targetHighlightObject = pickup.gameObject;
            }
            else if (player && CanSelectPlayersAndProps())
            {
                targetHighlightObject = player.gameObject;
            }
            else if (spawnable && (CanSelectPlayersAndProps() 
                                   || PlayerSetup.Instance.propGuidForSpawn == PlayerSetup.PropModeDeleteString))
            {
                targetHighlightObject = spawnable.gameObject;
            }
            
            // Check if the target changed
            if (targetHighlightObject == lastTarget)
                return;
            
            // Set or clear the target highlight
            SetTargetHighlight(targetHighlightObject);
        }

        private void SendRayTargetToMenu(CVRSpawnable spawnable, PlayerDescriptor player)
        {
            if (spawnable)
            {
                CVR_MenuManager.Instance.SetHandTarget("spawnable", spawnable.guid, spawnable.instanceId, spawnable.transform.position, "", hand);
                if (triggerHoverEvents) CVR_MenuManager.Instance.SetViewTarget("spawnable", spawnable.guid, spawnable.instanceId, spawnable.transform.position, "");
            }
            else if (player)
            {
                CVR_MenuManager.Instance.SetHandTarget("player", player.ownerId, "", player.transform.position, player.userName, hand);
                if (triggerHoverEvents) CVR_MenuManager.Instance.SetViewTarget("player", player.ownerId, "", player.transform.position, player.userName);
            }
            else
            {
                CVR_MenuManager.Instance.SetHandTarget("world_point", "", "", _hit.point, "", hand);
                if (triggerHoverEvents) CVR_MenuManager.Instance.SetViewTarget("world_point", "", "", _hit.point, "");
            }
        }

        private void HandlePropSpawn()
        {
            if (PlayerSetup.Instance.GetCurrentPropSelectionMode() != PlayerSetup.PropSelectionMode.Spawn)
                return;

            if (_hitUIInternal) return;

            RaycastHit waterRayHit;
            RaycastHit generalRayHit;
            bool waterHit = Physics.Raycast(rayDirectionTransform.TransformPoint(RayDirection * -0.15f),
                rayDirectionTransform.TransformDirection(RayDirection), out waterRayHit, MAX_RAYCAST_LENGTH, 1 << CVRLayers.Water,
                QueryTriggerInteraction.Collide);
            bool generalHit = Physics.Raycast(rayDirectionTransform.TransformPoint(RayDirection * -0.15f),
                rayDirectionTransform.TransformDirection(RayDirection), out generalRayHit, MAX_RAYCAST_LENGTH, generalMask,
                QueryTriggerInteraction.Ignore);

            if (waterHit && generalHit)
                if (waterRayHit.distance < generalRayHit.distance) _hit = waterRayHit;
                else _hit = generalRayHit;
            else if (waterHit) _hit = waterRayHit;
            else if (generalHit) _hit = generalRayHit;
            
            if (_interactDown && (waterHit || generalHit))
            {
                PlayerSetup.Instance.SpawnProp(PlayerSetup.Instance.propGuidForSpawn, _hit.point);
            }
        }

        private void TeleTargetSelection()
        {
            if (_telepathicPickupLocked || !_enableTelepathicGrab) return;
            
            RaycastHit teleHit = SphereCast(0.15f, 0.3f, 100f);
            if ((teleHit.collider != null)
                && (teleHit.collider.transform.parent != null))
            {
                Pickupable telePickup = teleHit.collider.transform.GetComponentInParent<Pickupable>();
                if (telePickup != null
                    && telePickup.CanPickup
                    && (telePickup.MaxGrabDistance >= teleHit.distance)
                    && !telePickup.IsGrabbedByMe)
                {
                    if ((_telepathicPickupCandidate != null)
                        && (telePickup != _telepathicPickupCandidate))
                        ClearTelepathicGrabTargetHighlight();

                    _telepathicPickupCandidate = telePickup;
                    _telepathicPickupTargeted = true;
                }
            }
        }

        private void LateUpdate()
        {
            // TODO: ControllerRay is being rewritten so this is a temporary addition
            Raycast();
            DisplayLineRendererIfNeeded();
        }

        private void Raycast()
        {
            UpdateBackupRay();

            UpdateInteractionMask();

            ResetPointAtUI();
            DisableLineRenderer();
            
            if (!enabled || !IsTracking())
                return;

            UpdateInputs();

            CheckExitPropModes();

            HandleTelepathicGrip();

            HandleGrabbedObjects();
            
            bool interactedWithMainMenu = HandleMenuUIInteraction();
            HandleMenuUIInteractionRelease();
            if (interactedWithMainMenu)
                return; // interacting with UI

            if (!CVR_InteractableManager.enableInteractions)
                return; // interactions disabled (in calibration or downed by combat)
            
            if (grabbedObject != null) return;

            Pickupable pickup = null;
            PlayerDescriptor player = null;
            Interactable interactable = null;
            CVRSpawnable spawnable = null;

            bool targetHasComponent = false;
            bool targetShouldHighlight = false;
            bool proxyGrab = false;
            bool foundTargets = FindTargets(out _hitUIInternal);
            if (foundTargets)
            {
                spawnable = HandleSpawnableClicked();
                targetHasComponent |= spawnable;
                
                pickup = CheckPickupDirect();
                targetHasComponent |= pickup;

                player = HandlePlayerClicked();
                targetHasComponent |= player;

                interactable = HandleInteractable();
                targetHasComponent |= interactable;



                targetShouldHighlight |= HandleUnityUI();
                targetHasComponent |= targetShouldHighlight;

                HandleIndirectUnityUI();

            }

            if (!pickup && !_hitUIInternal && !isDesktopRay && !isHeadRay)
            {
                proxyGrab = ProximityTargetSelection(ref pickup);
                if (!proxyGrab && !targetHasComponent) TeleTargetSelection();
            }

            GrabObject(pickup, proxyGrab);

            HandlePropSpawn();

            if (foundTargets && !proxyGrab) DisplayRayHighlight(targetShouldHighlight || interactedWithMainMenu || _hitUIInternal, player, interactable, spawnable, pickup);

            DisplayAuraHighlight(pickup, proxyGrab, player, interactable, spawnable);

            SendRayTargetToMenu(spawnable, player);
        }

        #region Where Am I Pointing
        
        private const float ORIGINAL_ALPHA = 0.502f;
        private const float INTERACTION_ALPHA = 0.1f;

        private void DisplayLineRendererIfNeeded()
        {
            if (isDesktopRay 
                || !enabled 
                || !IsTracking() 
                || !lineRenderer)
                return;

            UpdateLineRendererAlpha();

            if (lineRenderer.enabled 
                || !ShouldOverrideLineRenderer())
                return;

            UpdateLineRendererPosition();
        }
        
        private void UpdateLineRendererAlpha()
        {
            Material material = lineRenderer.material;
            Color color = material.color;

            bool anyMenuOpen = ViewManager.Instance.IsAnyMenuOpen;
            float targetAlpha = (!anyMenuOpen || uiActive) ? ORIGINAL_ALPHA : INTERACTION_ALPHA;
            if (!(Math.Abs(color.a - targetAlpha) > float.Epsilon)) return;
            
            color.a = targetAlpha;
            material.color = color;
        }

        private bool ShouldOverrideLineRenderer()
        {
            if (!ViewManager.Instance.IsAnyMenuOpen)
                return false;

            if (CVR_MenuManager.Instance.IsQuickMenuOpen 
                && hand == CVR_MenuManager.Instance.SelectedQuickMenuHand)
                return false;

            return true;
        }

        private void UpdateLineRendererPosition()
        {
            Vector3 rayOrigin = rayDirectionTransform.position;
            Vector3 rayEnd = rayOrigin + rayDirectionTransform.forward * MAX_RAYCAST_LENGTH;

            lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(rayOrigin));
            lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(rayEnd));
            lineRenderer.enabled = true;
        }
        
        #endregion Where Am I Pointing
        
        private void GrabObject(Pickupable pickup, RaycastHit hit)
        {
            if (!pickup.CanPickup)
                return;
            
            //UpdateGrabDistance(attachmentDistance);
            pivotPoint.rotation = pickup.transform.rotation;
            pickup.Grab(new InteractionContext(this), this, hit.point);
            grabbedObject = pickup;
            ClearTargetHighlight();
        }

        private Vector3 GetHandProximityCenterPosition()
        {
            Vector3 handPosition = transform.position;

            // Offset the detection center forward, so we don't grab stuff behind our writs
            handPosition += transform.forward * (ProximityDetectionRadius * 0.25f);

            // Offset the detection center away from the palm, so we don't grab stuff behind our hand palm
            Vector3 palmOffset = transform.right * (ProximityDetectionRadius * 0.75f);
            if (hand == CVRHand.Left)
                handPosition += palmOffset;
            else
                handPosition -= palmOffset;

            return handPosition;
        }
        
        private void GrabObject(Pickupable pickup, Vector3 pos)
        {
            if (!pickup.CanPickup)
                return;
            
            //UpdateGrabDistance(attachmentDistance);
            pivotPoint.rotation = pickup.transform.rotation;
            pickup.Grab(new InteractionContext(this), this, pos);
            grabbedObject = pickup;
            ClearTargetHighlight();
        }

        private bool ProximityTargetSelection(ref Pickupable selectedPickup)
        {
            if (!isInteractionRay || !_proximityGrabEnabled) return false;

            float detectionRadius = ProximityDetectionRadius;
            Vector3 handPosition = GetHandProximityCenterPosition();
            int generalLayersMask = generalMask & ~(1 << CVRLayers.MirrorReflection) & ~(1 << CVRLayers.UIInternal);

            if (_proximityGrabVisualizers)
                RuntimeGizmos.DrawSphere(handPosition, ProximityDetectionRadius*2, Color.blue, CVRLayers.UIInternal, 0.2f);

            // Reset results
            bool foundPickup = false;
            _proximityColliderClosestPickup = null;
            float closestPickupDistance = float.MaxValue;
            GameObject closestPickupHitObject = null;

            // Mark as processed this frame to prevent clearing the pickup highlight the next frame
            _proximityCalculatedThisFrame = true;

            // Detect all colliders within the detection radius that can have pickups
            int overlapCount = Physics.OverlapSphereNonAlloc(handPosition, detectionRadius, _proximityColliders, generalLayersMask, QueryTriggerInteraction.Collide);
            if (overlapCount <= 0)
            {
                ClearProximityGrabTargetHighlight();
                return false;
            }

            // Calculate the distance of the results
            for (int i = 0; i < overlapCount; i++)
            {
                Collider colliderCandidate = _proximityColliders[i];

                // Look for the closest pickup that is within grab reach
                if (colliderCandidate.TryGetComponent(out Pickupable pickupCandidate)
                    && pickupCandidate.CanPickup)
                    //&& pickupCandidate.IsWithinGrabReach(handPosition))
                {
                    float pickupDistance = Vector3.Distance(handPosition, pickupCandidate.RootTransform.position);
                    if (pickupDistance < closestPickupDistance)
                    {
                        closestPickupDistance = pickupDistance;
                        _proximityColliderClosestPickup = pickupCandidate;
                        closestPickupHitObject = colliderCandidate.gameObject;
                        foundPickup = true;
                    }
                }
            }

            if (foundPickup)
            {
                selectedPickup = _proximityColliderClosestPickup;
                SetProximityGrabTargetHighlight(closestPickupHitObject);
                return true;
            }

            ClearProximityGrabTargetHighlight();
            return false;
        }

        public void DropObject(bool force = false)
        {
            if (grabbedObject == null) return;
            if (grabbedObject.IsAutoHold && !force) return;
            grabbedObject.Drop(new InteractionContext(this));
            grabbedObject = null;
            pivotPoint.localRotation = Quaternion.identity;
        }

        #region Highlight Handling

        private void SetTargetHighlight(GameObject hit)
        {
            ClearTargetHighlight();
            
            if (hit == null) return;

            // if (triggerGazeEvents)
            // {
            //     var interactables = hit.GetComponentsInChildren<Interactable>();
            //     foreach (var interactable in interactables)
            //     {
            //         interactable.isLookedAt = true;
            //     }
            // }

            if (PlayerSetup.Instance.GetCurrentPropSelectionMode() == PlayerSetup.PropSelectionMode.Delete)
            {
                CVRSpawnable spawnable = hit.GetComponentInParent<CVRSpawnable>();
                if (spawnable != null && spawnable.gameObject != hit) hit = spawnable.gameObject;
            }

            if (!isInteractionRay) return;

            if (_enableHighlight)
            {
                if (!hit.TryGetComponent(out HighlightEffect highlight))
                {
                    highlight = hit.AddComponent<HighlightEffect>();

                    var foundRenderers = new List<Renderer>(8);
                    var stack = new Stack<Transform>();
                    stack.Push(hit.transform);

                    while (stack.Count > 0)
                    {
                        Transform current = stack.Pop();
                        
                        // skip things that *should* highlight their own children
                        if (current.TryGetComponent(out Pickupable _)) continue;
                        if (current.TryGetComponent(out CVRInteractable _)) continue;
                        
                        // add found renderers
                        if (current.TryGetComponent(out Renderer render)) foundRenderers.Add(render);
                        
                        // add children to stack
                        for (int i = 0; i < current.childCount; i++) stack.Push(current.GetChild(i));
                    }

                    highlight.SetTargets(hit.transform, foundRenderers.ToArray());
                }

                highlight.ProfileLoad(MetaPort.Instance.worldHighlightProfile);
                highlight.Refresh();
                highlight.SetHighlighted(true);
            }

            lastTarget = hit;
        }

        private void GrabObject(Pickupable pickup, bool foundProximityGrab)
        {
            if (pickup == null || !_isTryingToPickup) return;

            if (foundProximityGrab) GrabObject(pickup, GetHandProximityCenterPosition());
            else GrabObject(pickup, _hit);
        }
        
        private void SetTelepathicGrabTargetHighlight(GameObject hit)
        {
            if (!isInteractionRay) return;

            ClearTelepathicGrabTargetHighlight();

            if (hit == null) return;

            if (!_enableHighlight) return;

            if (!hit.TryGetComponent(out HighlightEffect highlight))
                highlight = hit.AddComponent<HighlightEffect>();

            highlight.ProfileLoad(MetaPort.Instance.worldHighlightProfile);
            highlight.Refresh();
            highlight.SetHighlighted(true);

            lastTelepathicGrabTarget = hit;
        }

        private void ClearTargetHighlight()
        {
            // There is no previous object to remove the highlight
            if (lastTarget == null) return;

            // If there is no other source for the object highlight, remove it
            if (lastTarget != lastTelepathicGrabTarget &&
                (!_telepathicPickupLocked || _telepathicPickupCandidate == null || lastTarget != _telepathicPickupCandidate.gameObject) &&
                lastTarget != lastProximityGrabTarget)
            {
                if (lastTarget.TryGetComponent(out HighlightEffect highlight))
                    highlight.SetHighlighted(false);
            }

            // var interactables = lastTarget.GetComponentsInChildren<Interactable>();
            // foreach (var interactable in interactables)
            // {
            //     interactable.isLookedAt = false;
            // }

            lastTarget = null;
        }
        
        private void ClearTelepathicGrabTargetHighlight()
        {
            if (!isInteractionRay) return;

            // There is no previous object to remove the highlight
            if (lastTelepathicGrabTarget == null) return;

            // If there is no other source for the object highlight, remove it
            if (lastTelepathicGrabTarget != lastTarget &&
                lastTelepathicGrabTarget != lastProximityGrabTarget)
            {
                if (lastTelepathicGrabTarget.TryGetComponent(out HighlightEffect highlight))
                    highlight.SetHighlighted(false);
            }

            lastTelepathicGrabTarget = null;
        }

        private void SetProximityGrabTargetHighlight(GameObject proximityPickupGameObject)
        {
            ClearProximityGrabTargetHighlight();

            if (!isInteractionRay || !_enableHighlight) return;

            if (!proximityPickupGameObject.TryGetComponent(out HighlightEffect highlight))
                highlight = proximityPickupGameObject.AddComponent<HighlightEffect>();

            highlight.ProfileLoad(MetaPort.Instance.worldHighlightProfile);
            highlight.Refresh();
            highlight.SetHighlighted(true);

            lastProximityGrabTarget = proximityPickupGameObject;
        }

        private void ClearProximityGrabTargetHighlight()
        {
            if (!isInteractionRay) return;

            // There is no previous object to remove the highlight
            if (lastProximityGrabTarget == null) return;

            // If there is no other source for the object highlight, remove it
            if (lastProximityGrabTarget != lastTarget &&
                lastProximityGrabTarget != lastTelepathicGrabTarget)
            {
                if (lastProximityGrabTarget.TryGetComponent(out HighlightEffect highlight))
                    highlight.SetHighlighted(false);
            }

            lastProximityGrabTarget = null;
        }

        #endregion

        private Vector2 GetScreenPositionFromRaycastHit(RaycastHit hit, RectTransform rect)
        {
            Vector2 position;

            var activeCamera = PlayerSetup.Instance.activeCam;

            var screenPosition = activeCamera.WorldToScreenPoint(hit.point);
                            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                new Vector2(screenPosition.x, screenPosition.y),
                activeCamera, 
                out position
            );

            return position;
        }
        
        private void SetSliderValueFromRay(Slider slider, RaycastHit hit, RectTransform rect)
        {
            if (rect == null) return;
            
            Vector2 position = GetScreenPositionFromRaycastHit(hit, rect);

            float valueX = Mathf.InverseLerp(rect.rect.min.x, rect.rect.max.x, position.x);
            float valueY = Mathf.InverseLerp(rect.rect.min.y, rect.rect.max.y, position.y);

            if (rect.rect.width > rect.rect.height)
            {
                slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, Mathf.Clamp01(valueX));
            }
            else
            {
                slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, Mathf.Clamp01(valueY));
            }
        }
        
        private void SetScrollViewValueFromRay(ScrollRect scrollRect, RaycastHit hit)
        {
            Vector2 position = GetScreenPositionFromRaycastHit(hit, scrollRect.viewport);

            Vector2 scrollOffset = scrollStartPositionView - position;

            Vector2 finalOffset = new Vector2(
                Mathf.Clamp(scrollStartPositionContent.x - scrollOffset.x, (scrollRect.content.rect.width - scrollRect.viewport.rect.width) * -1f, 0f),
                Mathf.Clamp(scrollStartPositionContent.y - scrollOffset.y, 0f, scrollRect.content.rect.height - scrollRect.viewport.rect.height)
            );
            
            if (!scrollRect.horizontal || scrollRect.content.rect.width <= scrollRect.viewport.rect.width) finalOffset.x = scrollStartPositionContent.x;
            if (!scrollRect.vertical || scrollRect.content.rect.height <= scrollRect.viewport.rect.height) finalOffset.y = scrollStartPositionContent.y;

            scrollRect.content.anchoredPosition = finalOffset;

            //float valueX = Mathf.InverseLerp(scrollView.content.rect.min.x, scrollView.content.rect.max.x, scrollOffset.x - scrollStartPositionContent.x);
            //float valueY = Mathf.InverseLerp(scrollView.content.rect.min.y, scrollView.content.rect.max.y, scrollOffset.y - scrollStartPositionContent.y);

            //scrollView.horizontalScrollbar.value = valueX;
            //scrollView.verticalScrollbar.value = valueY;
        }

        private void OnDrawGizmos()
        {
            if (isInteractionRay) Gizmos.DrawWireSphere(rayDirectionTransform.TransformPoint(RayDirection * 0.15f), 0.15f);
        }

        public void SetRayScale(float scale)
        {
            if (lineRenderer != null)
                lineRenderer.widthMultiplier = 0.005f * scale;
        }


        [AvailableToScripting]
        private bool IsTracking()
        {
            // if is desktop ray, always return true
            if (isDesktopRay)
                return true;
            
            // if is head ray, only return true if both controllers are not tracking
            if (isHeadRay)
                return !CVRInputManager.Instance.IsLeftControllerTracking() &&
                       !CVRInputManager.Instance.IsRightControllerTracking();

            // if neither head or desktop ray, return true if the controller is tracking
            return hand == CVRHand.Left
                ? CVRInputManager.Instance.IsLeftControllerTracking()
                : CVRInputManager.Instance.IsRightControllerTracking();
        }

    }
}*/