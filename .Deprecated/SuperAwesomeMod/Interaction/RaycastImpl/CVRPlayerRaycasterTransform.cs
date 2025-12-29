using UnityEngine;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
    public class CVRPlayerRaycasterTransform : CVRPlayerRaycaster
    {
        #region Proximity Grab
        
        public const float ProximityGrabRadiusScaleDefault = 0.1f;
        private float _proximityDetectionRadiusRelativeValue = ProximityGrabRadiusScaleDefault;
        private float ProximityDetectionRadius => _proximityDetectionRadiusRelativeValue * PlayerSetup.Instance.GetPlaySpaceScale();
        
        #endregion Proximity Grab

        #region Constructor

        public CVRPlayerRaycasterTransform(Transform rayOrigin, CVRHand hand) : base(rayOrigin) { _hand = hand; }

        private readonly CVRHand _hand;

        #endregion Constructor
        
        #region Overrides
        
        protected override Ray GetRayFromImpl() => new(_rayOrigin.position, _rayOrigin.forward);

        protected override Ray GetProximityRayFromImpl()
        {
            Vector3 handPosition = _rayOrigin.position;
            Vector3 handRight = _rayOrigin.right;

            // Offset the detection center forward, so we don't grab stuff behind our writs
            handPosition += _rayOrigin.forward * (ProximityDetectionRadius * 0.25f);

            // Offset the detection center away from the palm, so we don't grab stuff behind our hand palm
            Vector3 palmOffset = handRight * (ProximityDetectionRadius * 0.75f);
            if (_hand == CVRHand.Left)
                handPosition += palmOffset;
            else
                handPosition -= palmOffset;

            return new Ray(handPosition, _hand == CVRHand.Left ? handRight : -handRight);
        }

        #endregion Overrides
    }
}