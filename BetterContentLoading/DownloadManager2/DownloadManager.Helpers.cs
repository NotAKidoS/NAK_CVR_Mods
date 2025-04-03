using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using UnityEngine;

namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    internal static bool TryGetPlayerEntity(string playerId, out CVRPlayerEntity playerEntity)
    {
        CVRPlayerEntity player = CVRPlayerManager.Instance.NetworkPlayers.Find(p => p.Uuid == playerId);
        if (player == null)
        {
            // BetterContentLoadingMod.Logger.Error($"Player entity not found for ID: {playerId}");
            playerEntity = null;
            return false;
        }
        playerEntity = player;
        return true;
    }
    
    internal static bool TryGetPropData(string instanceId, out CVRSyncHelper.PropData propData)
    {
        CVRSyncHelper.PropData prop = CVRSyncHelper.Props.Find(p => p.InstanceId == instanceId);
        if (prop == null)
        {
            // BetterContentLoadingMod.Logger.Error($"Prop data not found for ID: {instanceId}");
            propData = null;
            return false;
        }
        propData = prop;
        return true;
    }

    private static bool IsPlayerLocal(string playerId)
    {
        return playerId == MetaPort.Instance.ownerId;
    }

    private static bool IsPlayerFriend(string playerId)
    {
        return Friends.FriendsWith(playerId);
    }

    private bool IsPlayerWithinPriorityDistance(CVRPlayerEntity player)
    {
        if (player.PuppetMaster == null) return false;
        return player.PuppetMaster.animatorManager.DistanceTo < PriorityDownloadDistance;
    }
    
    internal bool IsPropWithinPriorityDistance(CVRSyncHelper.PropData prop)
    {
        Vector3 propPosition = new(prop.PositionX, prop.PositionY, prop.PositionZ);
        return Vector3.Distance(propPosition, PlayerSetup.Instance.GetPlayerPosition()) < PriorityDownloadDistance;
    }
}