using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Core.UI;
using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.UI;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public abstract class CVRPlayerRaycaster
    {
        #region Enums

        [Flags]
        public enum RaycastFlags
        {
            None = 0,
            TelepathicCandidate = 1 << 0,
            ProximityInteract = 1 << 1,
            RayInteract = 1 << 2,
            CohtmlInteract = 1 << 3,
            All = ~0
        }

        #endregion Enums
        
        #region Constants
        
        private const float MAX_RAYCAST_DISTANCE = 100f;        // Max distance you can raycast
        private const float RAYCAST_SPHERE_RADIUS = 0.1f;       // Radius of the proximity sphere
        private const float TELEPATHIC_SPHERE_RADIUS = 0.3f;    // Radius of the telepathic sphere
        private const float MAX_TELEPATHIC_DISTANCE = 20f;      // Max distance for telepathic grab
        private const int MAX_RAYCAST_HITS = 100; // Hit buffer size, high due to triggers, which we use lots in CCK
        
        // Global setting is Collide, but better to be explicit about what we need
        private const QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
        
        // Layers that are reserved for other purposes or illegal to interact with
        private const int RESERVED_OR_ILLEGAL_LAYERS = (1 << CVRLayers.IgnoreRaycast)
                                                       | (1 << CVRLayers.MirrorReflection) 
                                                       | (1 << CVRLayers.PlayerLocal);

        #endregion Constants

        #region Static Fields

        private static readonly RaycastHit[] _hits = new RaycastHit[MAX_RAYCAST_HITS];
        private static readonly Comparer<RaycastHit> _hitsComparer = Comparer<RaycastHit>.Create((hit1, hit2) =>
        {
            bool isUI1 = hit1.collider.gameObject.layer == CVRLayers.UIInternal;
            bool isUI2 = hit2.collider.gameObject.layer == CVRLayers.UIInternal;

            // Prioritize UIInternal hits
            if (isUI1 && !isUI2) return -1;     // UIInternal comes first
            if (!isUI1 && isUI2) return 1;      // Non-UIInternal comes after

            // If both are UIInternal or both are not, sort by distance
            return hit1.distance.CompareTo(hit2.distance);
        });
        
        private static readonly LayerMask _telepathicLayerMask = 1 << CVRLayers.MirrorReflection;

        // Working variables to avoid repeated allocations
        private static Collider _workingCollider;
        private static GameObject _workingGameObject;
        private static Pickupable _workingPickupable;
        private static Interactable _workingInteractable;
        private static Selectable _workingSelectable;
        private static ICanvasElement _workingCanvasElement;

        #endregion Static Fields

        #region Private Fields

        private LayerMask _layerMask; // Default to no layers so we know if we fucked up

        #endregion Private Fields

        #region Constructor

        protected CVRPlayerRaycaster(Transform rayOrigin) => _rayOrigin = rayOrigin;
        protected readonly Transform _rayOrigin;

        #endregion Constructor
        
        #region Public Methods
        
        public void SetLayerMask(LayerMask layerMask)
        {
            layerMask &= ~RESERVED_OR_ILLEGAL_LAYERS;
            _layerMask = layerMask;
        }
        
        public CVRRaycastResult GetRaycastResults(RaycastFlags flags = RaycastFlags.All)
        {
            // Early out if we don't want to do anything
            if (flags == RaycastFlags.None) return default;
            
            Ray ray = GetRayFromImpl();
            CVRRaycastResult result = new();

            // Always check COHTML first
            if ((flags & RaycastFlags.CohtmlInteract) != 0 
                && TryProcessCohtmlHit(ray, ref result)) 
                return result;

            // Check if there are pickups or interactables in immediate proximity
            if ((flags & RaycastFlags.ProximityInteract) != 0)
            {
                Ray proximityRay = GetProximityRayFromImpl();
                ProcessProximityHits(proximityRay, ref result);
                if (result.isProximityHit) 
                    return result;
            }
            
            // Check for regular raycast hits
            if ((flags & RaycastFlags.RayInteract) != 0)
                ProcessRaycastHits(ray, ref result);
            
            // If we hit something, check for telepathic grab candidates at the hit point
            if ((flags & RaycastFlags.TelepathicCandidate) != 0 && result.hit.collider) 
                ProcessTelepathicGrabCandidate(result.hit.point, ref result);

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static bool TryProcessCohtmlHit(Ray ray, ref CVRRaycastResult result)
        {
            CohtmlControlledView hitView = CohtmlViewInputHandler.Instance.RayToView(ray, 
                out float _, out Vector2 hitCoords);
            if (hitView == null) return false;

            result.hitCohtml = true;
            result.hitCohtmlView = hitView;
            result.hitCohtmlCoords = hitCoords;
            
            // Manually check for pickups & interactables on the hit view (future-proofing for menu grabbing)
            if (hitView.TryGetComponent(out _workingInteractable)) result.hitInteractable = _workingInteractable;
            if (hitView.TryGetComponent(out _workingPickupable)) result.hitPickupable = _workingPickupable;
            
            return true;
        }

        private void ProcessProximityHits(Ray ray, ref CVRRaycastResult result)
        {
            int proximityHits = Physics.SphereCastNonAlloc(
                ray.origin,
                RAYCAST_SPHERE_RADIUS,
                ray.direction,
                _hits,
                0.001f,
                _layerMask,
                _triggerInteraction
            );

            if (proximityHits <= 0) return;

            Array.Sort(_hits, 0, proximityHits, _hitsComparer);

            for (int i = 0; i < proximityHits; i++)
            {
                RaycastHit hit = _hits[i];
                _workingCollider = hit.collider;
                _workingGameObject = _workingCollider.gameObject;
                    
                // Skip things behind the ray origin
                if (Vector3.Dot(ray.direction, hit.point - ray.origin) < 0)
                    continue;

                // Check for interactables & pickupables in proximity
                if (!TryProcessInteractables(hit, ref result))
                    continue;
                    
                result.isProximityHit = true;
                break;
            }
        }

        private void ProcessRaycastHits(Ray ray, ref CVRRaycastResult result)
        {
            // Get all hits including triggers, sorted by UI Internal layer & distance
            int hitCount = Physics.RaycastNonAlloc(ray, 
                _hits, 
                MAX_RAYCAST_DISTANCE, 
                _layerMask, 
                _triggerInteraction);
            
            if (hitCount <= 0) return;
            
            Array.Sort(_hits, 0, hitCount, _hitsComparer);
            
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hits[i];
                _workingCollider = hit.collider;
                _workingGameObject = _workingCollider.gameObject;
                
                // Special case where we only get the closest water hit position.
                // As the array is sorted by distance, we only need to check if we didn't hit water yet.
                if (!result.hitWater) TryProcessFluidVolume(hit, ref result);
                
                // Check for hits in order of priority
                
                if (TryProcessSelectable(hit, ref result)) 
                    break; // Hit a Unity UI Selectable (Button, Slider, etc.)
                
                if (TryProcessCanvasElement(hit, ref result))
                    break; // Hit a Unity UI Canvas Element (ScrollRect, idk what else yet)
                
                if (TryProcessInteractables(hit, ref result)) 
                    break; // Hit an in-range Interactable or Pickup
                
                if (TryProcessWorldHit(hit, ref result))
                    break; // Hit a non-trigger collider (world, end of ray)
            }
        }
        
        private void ProcessTelepathicGrabCandidate(Vector3 hitPoint, ref CVRRaycastResult result)
        {
            // If we already hit a pickupable, we don't need to check for telepathic grab candidates
            if (result.hitPickupable)
            {
                result.hasTelepathicGrabCandidate = true;
                result.telepathicPickupable = result.hitPickupable;
                result.telepathicGrabPoint = hitPoint;
                return;
            }
            
            // If the hit distance is too far, don't bother checking for telepathic grab candidates
            if (Vector3.Distance(hitPoint, _rayOrigin.position) > MAX_TELEPATHIC_DISTANCE)
                return;
            
            // Check for mirror reflection triggers in a sphere around the hit point
            int telepathicHits = Physics.SphereCastNonAlloc(
                hitPoint,
                TELEPATHIC_SPHERE_RADIUS,
                Vector3.up,
                _hits,
                0.001f,
                _telepathicLayerMask,
                QueryTriggerInteraction.Collide
            );
            
            if (telepathicHits <= 0) return;

            // Look for pickupable objects near our hit point
            var nearestDistance = float.MaxValue;
            for (int i = 0; i < telepathicHits; i++)
            {
                RaycastHit hit = _hits[i];
                _workingCollider = hit.collider;
                // _workingGameObject = _workingCollider.gameObject;
                
                Transform parentTransform = _workingCollider.transform.parent;
                if (!parentTransform
                    || !parentTransform.TryGetComponent(out _workingPickupable) 
                    || !_workingPickupable.CanPickup)
                    continue;

                var distance = Vector3.Distance(hitPoint, hit.point);
                if (!(distance < nearestDistance))
                    continue;
                    
                result.hasTelepathicGrabCandidate = true;
                result.telepathicPickupable = _workingPickupable;
                result.telepathicGrabPoint = hitPoint;
                nearestDistance = distance;
            }
        }

        private static bool TryProcessSelectable(RaycastHit hit, ref CVRRaycastResult result)
        {
            if (!_workingGameObject.TryGetComponent(out _workingSelectable))
                return false;
            
            result.hitUnityUi = true;
            result.hitSelectable = _workingSelectable;
            result.hit = hit;
            return true;
        }
        
        private static bool TryProcessCanvasElement(RaycastHit hit, ref CVRRaycastResult result)
        {
            if (!_workingGameObject.TryGetComponent(out _workingCanvasElement))
                return false;
            
            result.hitUnityUi = true;
            result.hitCanvasElement = _workingCanvasElement;
            result.hit = hit;
            return true;
        }
        
        private static void TryProcessFluidVolume(RaycastHit hit, ref CVRRaycastResult result)
        {
            if (_workingGameObject.layer != CVRLayers.Water) return;

            result.hitWater = true;
            result.waterHit = hit;
        }
            
        private static bool TryProcessInteractables(RaycastHit hit, ref CVRRaycastResult result)
        {
            bool hitValidComponent = false;
            
            if (_workingGameObject.TryGetComponent(out _workingInteractable) 
                && _workingInteractable.CanInteract
                && IsCVRInteractableWithinRange(_workingInteractable, hit))
            {
                result.hitInteractable = _workingInteractable;
                hitValidComponent = true;
            }
            
            if (_workingGameObject.TryGetComponent(out _workingPickupable) 
                && _workingPickupable.CanPickup
                && IsCVRPickupableWithinRange(_workingPickupable, hit))
            {
                result.hitPickupable = _workingPickupable;
                hitValidComponent = true;
            }

            if (!hitValidComponent) 
                return false;
            
            result.hit = hit;
            return true;
        }

        private static bool TryProcessWorldHit(RaycastHit hit, ref CVRRaycastResult result)
        {
            if (_workingCollider.isTrigger)
                return false;
                
            result.hitWorld = true;
            result.hit = hit;
            return true;
        }
        
        #endregion Private Methods
        
        #region Protected Methods
        
        protected abstract Ray GetRayFromImpl();
        protected abstract Ray GetProximityRayFromImpl();

        #endregion Protected Methods
        
        #region Utility Because Original Methods Are Broken

        private static bool IsCVRInteractableWithinRange(Interactable interactable, RaycastHit hit)
        {
            if (interactable is not CVRInteractable cvrInteractable) 
                return true;
            
            foreach (CVRInteractableAction action in cvrInteractable.actions)
            {
                if (action.actionType
                    is not (CVRInteractableAction.ActionRegister.OnInteractDown
                    or CVRInteractableAction.ActionRegister.OnInteractUp
                    or CVRInteractableAction.ActionRegister.OnInputDown
                    or CVRInteractableAction.ActionRegister.OnInputUp))
                    continue;
                
                float maxDistance = action.floatVal;
                if (Mathf.Approximately(maxDistance, 0f)
                    || hit.distance <= maxDistance)
                    return true; // Interactable is within range
            }
            return false;
        }
        
        private static bool IsCVRPickupableWithinRange(Pickupable pickupable, RaycastHit hit)
        {
            return hit.distance <= pickupable.MaxGrabDistance;
        }
        
        // private static bool IsCVRCanvasWrapperWithinRange(CVRCanvasWrapper canvasWrapper, RaycastHit hit)
        // {
        //     return hit.distance <= canvasWrapper.MaxInteractDistance;
        // }

        #endregion Utility Because Original Methods Are Broken
    }
}