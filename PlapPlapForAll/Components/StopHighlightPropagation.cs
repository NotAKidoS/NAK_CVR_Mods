using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace NAK.PlapPlapForAll;

public class StopHighlightPropagation : Pickupable
{
    public override void OnGrab(InteractionContext context, Vector3 grabPoint)
    {
        // throw new NotImplementedException();
    }

    public override void OnDrop(InteractionContext context)
    {
        // throw new NotImplementedException();
    }

    public override void OnUseDown(InteractionContext context)
    {
        // throw new NotImplementedException();
    }

    public override void OnUseUp(InteractionContext context)
    {
        // throw new NotImplementedException();
    }

    public override void FlingTowardsTarget(ControllerRay controllerRay)
    {
        // throw new NotImplementedException();
    }

    public override bool CanPickup { get; }
    public override bool DisallowTheft { get; }
    public override float MaxGrabDistance { get; }
    public override float MaxPushDistance { get; }
    public override bool IsAutoHold { get; }
    public override bool IsObjectRotationAllowed { get; }
    public override bool IsObjectPushPullAllowed { get; }
    public override bool IsTelepathicGrabAllowed { get; }
    public override bool IsObjectUseAllowed { get; }
}