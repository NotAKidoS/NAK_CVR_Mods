using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace NAK.LuaNetVars;

public class PickupableObject : Pickupable
{
    [SerializeField] private bool canPickup = true;
    [SerializeField] private bool disallowTheft = false;
    [SerializeField] private float maxGrabDistance = 2f;
    [SerializeField] private float maxPushDistance = 2f;
    [SerializeField] private bool isAutoHold = false;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private bool allowPushPull = true;
    [SerializeField] private bool allowInteraction = true;

    private PickupableBehaviour behaviour;
    private bool isInitialized;

    private void Awake()
    {
        // Generate a unique network ID based on the instance ID
        string networkId = $"pickup_{gameObject.name}";
        behaviour = new PickupableBehaviour(networkId, this);
        isInitialized = true;
    }

    private void OnDestroy()
    {
        behaviour?.Dispose();
    }

    private void Update()
    {
        if (behaviour?.HasOwnership == true)
        {
            transform.SetPositionAndRotation(ControllerRay.pivotPoint.position, ControllerRay.pivotPoint.rotation);
            behaviour.UpdateTransform(transform.position, transform.rotation);
        }
    }

    #region Pickupable Implementation
    
    public override void OnGrab(InteractionContext context, Vector3 grabPoint)
    {
        if (!isInitialized) return;
        behaviour.OnGrabbed(context);
    }

    public override void OnDrop(InteractionContext context)
    {
        if (!isInitialized) return;
        behaviour.OnDropped();
    }

    public override void OnFlingTowardsTarget(Vector3 target)
    {
        // ignore
    }

    public override bool CanPickup => canPickup;
    public override bool DisallowTheft => disallowTheft;
    public override float MaxGrabDistance => maxGrabDistance;
    public override float MaxPushDistance => maxPushDistance;
    public override bool IsAutoHold => isAutoHold;
    public override bool IsObjectRotationAllowed => allowRotation;
    public override bool IsObjectPushPullAllowed => allowPushPull;
    public override bool IsObjectInteractionAllowed => allowInteraction;
    
    #endregion Pickupable Implementation
}