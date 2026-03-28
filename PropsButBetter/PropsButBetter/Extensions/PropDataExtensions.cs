using ABI_RC.Core.Networking;
using ABI_RC.Core.Util;

namespace NAK.PropsButBetter;

public static class PropDataExtensions
{
    extension(CVRSyncHelper.PropData prop)
    {
        public bool IsSpawnedByMe()
            => prop.SpawnedBy == AuthManager.UserId;

        public void CopyFrom(CVRSyncHelper.PropData sourceData)
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

        public void RecycleSafe()
        {
            if (prop.IsSpawnedByMe()) 
                CVRSyncHelper.DeleteMyPropByInstanceIdOverNetwork(prop.InstanceId);
            prop.Recycle();
        }
    }
}