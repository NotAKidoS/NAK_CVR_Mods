using ABI_RC.Core.Networking;
using ABI_RC.Core.Util;
using ABI.CCK.Components;

namespace NAK.PropsButBetter;

public static class PropDataExtensions
{
    public static bool IsSpawnedByMe(this CVRSyncHelper.PropData prop)
        => prop.SpawnedBy == AuthManager.UserId;

    public static bool IsSpawnedByAdmin(this CVRSyncHelper.PropData prop)
        => prop.SpawnedBy is CVRSyncHelper.OWNERID_LOCALSERVER or CVRSyncHelper.OWNERID_SYSTEM;

    public static void CopyFrom(this CVRSyncHelper.PropData prop, CVRSyncHelper.PropData sourceData)
    {
        prop.ObjectId = sourceData.ObjectId;
        prop.InstanceId = sourceData.InstanceId;
        prop.PositionX = sourceData.PositionX;
        prop.PositionY = sourceData.PositionY;
        prop.PositionZ = sourceData.PositionZ;
        prop.RotationX = sourceData.RotationX;
        prop.RotationY = sourceData.RotationY;
        prop.RotationZ = sourceData.RotationZ;
        prop.ScaleX = sourceData.ScaleX;
        prop.ScaleY = sourceData.ScaleY;
        prop.ScaleZ = sourceData.ScaleZ;
        prop.CustomFloatsAmount = sourceData.CustomFloatsAmount;
        prop.CustomFloats  = sourceData.CustomFloats;
        prop.SpawnedBy = sourceData.SpawnedBy;
        prop.syncedBy = sourceData.syncedBy;
        prop.syncType = sourceData.syncType;
        prop.ContentMetadata = sourceData.ContentMetadata;
        prop.CanFireDecommissionEvents = sourceData.CanFireDecommissionEvents;
    }

    public static void RecycleSafe(this CVRSyncHelper.PropData prop)
    {
        if (prop.IsSpawnedByMe()) 
            CVRSyncHelper.DeleteMyPropByInstanceIdOverNetwork(prop.InstanceId);
        prop.Recycle();
    }
}