using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace NAK.RCCVirtualSteeringWheel;

public class SteeringWheelPickup : Pickupable
{
    #region Public Properties
    public override bool DisallowTheft => true;
    public override float MaxGrabDistance => 0.8f;
    public override float MaxPushDistance => 0f;
    public override bool IsAutoHold => false;
    public override bool IsObjectRotationAllowed => false;
    public override bool IsObjectPushPullAllowed => false;
    public override bool IsObjectUseAllowed => false;
    public override bool CanPickup => IsPickupable && !IsPickedUp;
    internal SteeringWheelRoot root;
    #endregion

    #region Public Methods
    public override void OnUseDown(InteractionContext context) { }
    public override void OnUseUp(InteractionContext context) { }
    public override void OnFlingTowardsTarget(Vector3 target) { }

    public override void OnGrab(InteractionContext context, Vector3 grabPoint) {
        if (ControllerRay?.pivotPoint != null)
            root.StartTrackingTransform(ControllerRay.transform);
    }

    public override void OnDrop(InteractionContext context) {
        if (ControllerRay?.transform != null)
            root.StopTrackingTransform(ControllerRay.transform);
    }
    #endregion
}