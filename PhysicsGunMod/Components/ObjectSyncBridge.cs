using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.PhysicsGunMod.Components;

public class ObjectSyncBridge : MonoBehaviour
{
    private PhysicsGunInteractionBehavior _physicsGun;
    
    private void Start()
    {
        // find physics gun
        if (!TryGetComponent(out _physicsGun))
        {
            PhysicsGunMod.Logger.Msg("Failed to find physics gun!");
            Destroy(this);
            return;
        }
        
        // listen for events
        _physicsGun.OnPreGrabbedObject = o =>
        {
            bool canTakeOwnership = false;

            //
            CVRObjectSync objectSync = o.GetComponentInParent<CVRObjectSync>();
            // if (objectSync != null 
            //     && (objectSync.SyncType == 0 // check if physics synced or synced by us
            //         || objectSync.SyncedByMe))
            //     canTakeOwnership = true;
            //
            CVRSpawnable spawnable = o.GetComponentInParent<CVRSpawnable>();
            // if (spawnable != null)
            // {
            //     CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(match => match.InstanceId == spawnable.instanceId);
            //     if (propData != null 
            //         && (propData.syncType == 0 // check if physics synced or synced by us
            //             || propData.syncedBy == MetaPort.Instance.ownerId))
            //         canTakeOwnership = true;
            // }
            //
            CVRPickupObject pickup = o.GetComponentInParent<CVRPickupObject>();
            // if (pickup != null
            //     && (pickup.grabbedBy == MetaPort.Instance.ownerId // check if already grabbed by us
            //         || pickup.grabbedBy == "" || !pickup.disallowTheft)) // check if not grabbed or allows theft
            //     canTakeOwnership = true;
            //
            // if (!canTakeOwnership // if we can't take ownership, don't grab, unless there is no syncing at all (local object)
            //     && (objectSync || spawnable || pickup ))
            //     return false;
            
            if (pickup)
            {
                pickup.GrabbedBy = MetaPort.Instance.ownerId;
                pickup._grabStartTime = Time.time;
            }
            if (spawnable) spawnable.isPhysicsSynced = true;
            if (objectSync) objectSync.isPhysicsSynced = true;

            return true;
        };
        
        _physicsGun.OnObjectReleased = o =>
        {
            // CVRObjectSync objectSync = o.GetComponentInParent<CVRObjectSync>();
            // if (objectSync != null && objectSync.SyncType == 0)
            //     objectSync.isPhysicsSynced = false;
            //
            // CVRSpawnable spawnable = o.GetComponentInParent<CVRSpawnable>();
            // if (spawnable != null)
            // {
            //     CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(match => match.InstanceId == spawnable.instanceId);
            //     if (propData != null && (propData.syncType == 0 || propData.syncedBy == MetaPort.Instance.ownerId))
            //         spawnable.isPhysicsSynced = false;
            // }
            
            // CVRPickupObject pickup = o.GetComponentInParent<CVRPickupObject>();
            // if (pickup != null && pickup.grabbedBy == MetaPort.Instance.ownerId)
            //     pickup.grabbedBy = "";

            return false;
        };
    }

    private void OnDestroy()
    {
        // stop listening for events
        _physicsGun.OnObjectGrabbed = null;
    }
}