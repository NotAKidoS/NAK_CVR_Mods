using UnityEngine;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.ModNetwork;

namespace NAK.LuaNetVars;

public class PickupableBehaviour : MNSyncedBehaviour
{
    private enum PickupMessageType : byte
    {
        GrabState,
        Transform
    }

    private bool isHeld;
    private string holderId;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    
    public PickupableObject Pickupable { get; private set; }
    
    public PickupableBehaviour(string networkId, PickupableObject pickupable) : base(networkId, autoAcceptTransfers: false)
    {
        Pickupable = pickupable;
        isHeld = false;
        holderId = string.Empty;
        lastPosition = pickupable.transform.position;
        lastRotation = pickupable.transform.rotation;
    }

    public void OnGrabbed(InteractionContext context)
    {
        RequestOwnership(success => {
            if (success)
            {
                isHeld = true;
                holderId = LocalUserId;
                SendNetworkedData(WriteGrabState);
            }
            else
            {
                // Ownership request failed, drop the object
                Pickupable.ControllerRay = null; // Force drop
            }
        });
    }

    public void OnDropped()
    {
        if (!HasOwnership) return;
        
        isHeld = false;
        holderId = string.Empty;
        SendNetworkedData(WriteGrabState);
    }

    public void UpdateTransform(Vector3 position, Quaternion rotation)
    {
        if (!HasOwnership || !isHeld) return;

        lastPosition = position;
        lastRotation = rotation;
        SendNetworkedData(WriteTransform);
    }

    protected override OwnershipResponse OnOwnershipRequested(string requesterId)
    {
        // If the object is held by the current owner, reject the transfer
        if (isHeld && holderId == LocalUserId)
            return OwnershipResponse.Rejected;

        // If theft is disallowed and the object is held by someone, reject the transfer
        if (Pickupable.DisallowTheft && !string.IsNullOrEmpty(holderId))
            return OwnershipResponse.Rejected;

        return OwnershipResponse.Accepted;
    }

    protected override void WriteState(ModNetworkMessage message)
    {
        message.Write(isHeld);
        message.Write(holderId);
        message.Write(lastPosition);
        message.Write(lastRotation);
    }

    protected override void ReadState(ModNetworkMessage message)
    {
        message.Read(out isHeld);
        message.Read(out holderId);
        message.Read(out lastPosition);
        message.Read(out lastRotation);
        
        UpdatePickupableState();
    }

    private void WriteGrabState(ModNetworkMessage message)
    {
        message.Write((byte)PickupMessageType.GrabState);
        message.Write(isHeld);
        message.Write(holderId);
    }

    private void WriteTransform(ModNetworkMessage message)
    {
        message.Write((byte)PickupMessageType.Transform);
        message.Write(lastPosition);
        message.Write(lastRotation);
    }

    protected override void ReadCustomData(ModNetworkMessage message)
    {
        message.Read(out byte messageType);
        
        switch ((PickupMessageType)messageType)
        {
            case PickupMessageType.GrabState:
                message.Read(out isHeld);
                message.Read(out holderId);
                break;

            case PickupMessageType.Transform:
                message.Read(out Vector3 position);
                message.Read(out Quaternion rotation);
                lastPosition = position;
                lastRotation = rotation;
                break;
        }

        UpdatePickupableState();
    }

    private void UpdatePickupableState()
    {
        // Update transform if we're not the holder
        if (!isHeld || holderId != LocalUserId)
        {
            Pickupable.transform.position = lastPosition;
            Pickupable.transform.rotation = lastRotation;
        }

        // Force drop if we were holding but someone else took ownership
        if (isHeld && holderId != LocalUserId)
        {
            Pickupable.ControllerRay = null; // Force drop
        }
    }

    protected override void OnOwnershipChanged(string newOwnerId)
    {
        base.OnOwnershipChanged(newOwnerId);
        
        // If we lost ownership and were holding, force drop
        if (!HasOwnership && holderId == LocalUserId)
        {
            isHeld = false;
            holderId = string.Empty;
            Pickupable.ControllerRay = null; // Force drop
        }
    }
}