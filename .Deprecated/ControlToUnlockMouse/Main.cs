using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Core.Player;
using System.Reflection;
using cohtml.Net;
using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Object = UnityEngine.Object;

namespace NAK.ControlToUnlockMouse;

public class ControlToUnlockMouseMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ControlToUnlockMouseMod));

    internal static readonly MelonPreferences_Entry<NoRotatePivotPoint> EntryOriginPivotPoint =
        Category.CreateEntry("no_rotate_pivot_point", NoRotatePivotPoint.Pickupable,
            "NoRotation Pickupable Pivot Point", "The pivot point to use when no rotation object is grabbed.");

    public enum NoRotatePivotPoint
    {
        Pickupable,
        AvatarHead,
        AvatarChest,
        AvatarClosestShoulder,
    }
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Awake),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnPlayerSetupAwake),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(CVR_MenuManager).GetMethod(nameof(CVR_MenuManager.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnMenuManagerStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.HandleUnityUI),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnControllerRayHandleUnityUIDirectAndIndirect),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.HandleIndirectUnityUI),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnControllerRayHandleUnityUIDirectAndIndirect),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.LateUpdate),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnPreControllerRayLateUpdate),
                BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyMethod(typeof(ControlToUnlockMouseMod).GetMethod(nameof(OnPostControllerRayLateUpdate),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnPlayerSetupAwake(PlayerSetup __instance)
    {
        // Get original fields
        LayerMask layerMask = __instance.desktopRay.generalMask;
        
        // Destroy the existing desktop ray
        Object.Destroy(__instance.desktopRay);
     
        // Get the desktop camera
        Camera desktopCam = __instance.desktopCam;
        
        // Create a new child object under the desktop camera for the ray
        GameObject rayObject = new("DesktopRay")
        {
            transform =
            {
                parent = desktopCam.transform,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            }
        };

        // Add ControllerRay component
        ControllerRay newRay = rayObject.AddComponent<ControllerRay>();
        newRay.isDesktopRay = true;
        newRay.isInteractionRay = true;
        newRay.RayDirection = Vector3.forward;
        newRay.generalMask = layerMask;
        newRay.hand = CVRHand.Right; // Important to even work
        newRay.attachmentDistance = 0f;
        newRay.currentAttachmentDistance = 0f;
        
        // Assign new ray to desktopRay field
        __instance.desktopRay = newRay;

        // Add our custom controller script
        DesktopRayController rayController = rayObject.AddComponent<DesktopRayController>();
        rayController.controllerRay = newRay;
        rayController.desktopCamera = desktopCam;
    }
    
    private static void OnMenuManagerStart(CVR_MenuManager __instance)
    {
        __instance.desktopControllerRay = PlayerSetup.Instance.desktopRay;
    }
    
    private static bool OnControllerRayHandleUnityUIDirectAndIndirect(ControllerRay __instance)
    {
        return !__instance.isDesktopRay || Cursor.lockState == CursorLockMode.Locked;
    }

    private static void OnPreControllerRayLateUpdate(ControllerRay __instance, ref bool __state)
    {
        if (!__instance.isDesktopRay) 
            return;
        
        ViewManager menu = ViewManager.Instance;
        __state = menu._gameMenuOpen;

        if (!__state) menu._gameMenuOpen = Cursor.lockState != CursorLockMode.Locked;
    }

    private static void OnPostControllerRayLateUpdate(ControllerRay __instance, ref bool __state)
    {
        if (!__instance.isDesktopRay) return;
        ViewManager.Instance._gameMenuOpen = __state;
    }
}

public class DesktopRayController : MonoBehaviour
{
    internal ControllerRay controllerRay;
    internal Camera desktopCamera;

    private void Update()
    {
        // Toggle desktop mouse mode based on Control key state
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!ViewManager.Instance.IsAnyMenuOpen) RootLogic.CursorLock(false);
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            if (!ViewManager.Instance.IsAnyMenuOpen) RootLogic.CursorLock(true);
        }
        
        Transform rayRoot = controllerRay.transform;
        Transform rayDirection = controllerRay.rayDirectionTransform;
        Transform attachment = controllerRay.attachmentPoint;
        Camera cam = desktopCamera;
        
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Reset local position when unlocked
            rayRoot.localPosition = Vector3.zero;
            rayRoot.localRotation = Quaternion.identity;
            
            // Reset local position and rotation when locked
            rayDirection.localPosition = new Vector3(0f, 0f, 0.001f);
            rayDirection.localRotation = Quaternion.identity;
        }
        else
        {
            bool isAnyMenuOpen = ViewManager.Instance.IsAnyMenuOpen;
            Pickupable grabbedObject = controllerRay.grabbedObject;
            
            // Only do when not holding an origin object
            Vector3 screenPos = new(Input.mousePosition.x, Input.mousePosition.y);
            
            if (isAnyMenuOpen)
            {
                // Center the ray
                rayRoot.localPosition = Vector3.zero;
            }
            else if (grabbedObject && !grabbedObject.IsObjectRotationAllowed)
            {
                // Specialized movement of ray around pickupable pivot
                Vector3 pivotPoint = grabbedObject.transform.position;
                Vector3 pivotPointCenter = grabbedObject.RootTransform.position;

                PlayerSetup playerSetup = PlayerSetup.Instance;
                if (playerSetup != null && playerSetup._animator != null && playerSetup._animator.isHuman)
                {
                    Animator animator = playerSetup._animator;
                    switch (ControlToUnlockMouseMod.EntryOriginPivotPoint.Value)
                    {
                        case ControlToUnlockMouseMod.NoRotatePivotPoint.AvatarHead:
                            {
                                Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                                if (headBone != null) pivotPoint = headBone.position;
                                break;
                            }
                        case ControlToUnlockMouseMod.NoRotatePivotPoint.AvatarChest:
                            {
                                if (playerSetup._avatar != null)
                                {
                                    Transform chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                                    if (chestBone != null) pivotPoint = chestBone.position;
                                }
                                break;
                            }
                        case ControlToUnlockMouseMod.NoRotatePivotPoint.AvatarClosestShoulder:
                            {
                                if (playerSetup._avatar != null)
                                {
                                    Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                                    Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                                    if (leftShoulder != null || rightShoulder != null)
                                    {
                                        if (leftShoulder != null && rightShoulder != null)
                                        {
                                            pivotPoint = Vector3.Distance(leftShoulder.position, pivotPoint) < Vector3.Distance(rightShoulder.position, pivotPoint)
                                                ? leftShoulder.position
                                                : rightShoulder.position;
                                        }
                                        else if (leftShoulder != null)
                                        {
                                            pivotPoint = leftShoulder.position;
                                        }
                                        else
                                        {
                                            pivotPoint = rightShoulder.position;
                                        }
                                    }
                                }
                                break;
                            }
                        case ControlToUnlockMouseMod.NoRotatePivotPoint.Pickupable:
                        default:
                            break;
                    }
                }
                
                // Get local position of pivotPoint relative to rayRoot
                // This is shit but i cant wrap my head around the proper way to compute this lol
                Vector3 localPivotPoint = rayRoot.InverseTransformPoint(pivotPoint);
                Vector3 localPivotPointCenter = rayRoot.InverseTransformPoint(pivotPointCenter);
                localPivotPoint.x = localPivotPointCenter.x; // Maintain local X
                localPivotPoint.y = localPivotPointCenter.y; // Maintain local Y
                
                // Compute target world position based on the mouse and attachment distance.
                screenPos.z = 10f;
                Vector3 targetWorldPos = cam.ScreenToWorldPoint(screenPos);
                
                // Desired direction from the pivot point (grabbed object) to the target world position.
                Vector3 directionToTarget = targetWorldPos - rayRoot.TransformPoint(localPivotPoint);;
                
                if (directionToTarget.sqrMagnitude < 1e-6f)
                    directionToTarget = rayRoot.forward; // Fallback if mouse is centered
                
                // Calculate the target rotation for rayRoot.
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, cam.transform.up);
                
                // Get the current local offset of the grabbed object relative to rayRoot.
                Vector3 localPickupOffset = rayRoot.InverseTransformPoint(pivotPoint);
                
                // Compute the new rayRoot position to keep the grabbed object (child) at pivotPoint.
                Vector3 newRayRootPos = pivotPoint - (targetRotation * localPickupOffset);
                
                // Apply the new rotation and position.
                rayRoot.rotation = targetRotation;
                rayRoot.position = newRayRootPos;
            }
            else
            {
                float distance;
                if (grabbedObject)
                {
                    // This position is calculated basically same way as below in BasePickupHandler,
                    // but not determined by ray hit
                    distance = attachment.localPosition.z;
                }
                else
                {
                    // Compute distance forward from ray
                    Vector3 localOffset = rayRoot.InverseTransformPoint(controllerRay._hit.point);
                    distance = localOffset.z;
                }

                screenPos.z = distance;
                
                // Compute world position from where mouse is on screen
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                
                // Normal movement of ray
                Vector3 newLocalPos = rayRoot.parent.InverseTransformPoint(worldPos);
                newLocalPos.z = rayRoot.localPosition.z; // Maintain local Z
                rayRoot.localPosition = newLocalPos;
            }
            
            // Compute mouse ray in world space
            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
            rayDirection.position = mouseRay.origin;
            rayDirection.rotation = Quaternion.LookRotation(mouseRay.direction, cam.transform.up);
        }
    }
}