using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.Jobs;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.Util;
using ABI_RC.Helpers;
using ABI_RC.Systems.Communications;
using ABI_RC.Systems.Communications.Settings;
using ABI_RC.Systems.GameEventSystem;
using DarkRift;
using HarmonyLib;
using UnityEngine;

namespace NAK.ReconnectionSystemFix.Patches;

internal static class NetworkManagerPatches
{
    internal static bool IsRecoveringFromReconnectionEvent;
    private static float LatestReconnectTime;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.OnGameNetworkConnected))]
    private static bool Prefix_NetworkManager_OnGameNetworkConnected(ref NetworkManager __instance)
    {
        if (Instances.IsReconnecting && !IsRecoveringFromReconnectionEvent)
        {
            IsRecoveringFromReconnectionEvent = true;
            SchedulerSystem.AddJob(CheckIfConnectionIsStable, 1f, 1f, -1);
        }
        
        LatestReconnectTime = Time.time;
        
        // CVRWorld.Start calls TryDeleteAllPlayers anyways ???
        
        // if (Instances.IsConnectingInitially) // reimplemented method to add this check, cause i cant do transpiler :(
        // {
        //     foreach (CVRPlayerEntity cvrplayerEntity in CVRPlayerManager.Instance.NetworkPlayers)
        //     {
        //         Object.Destroy(cvrplayerEntity.PlayerObject);
        //         cvrplayerEntity.Recycle();
        //     }
        //     CVRPlayerManager.Instance.NetworkPlayers.Clear();
        // }

        SchedulerSystem.RemoveJob(__instance.ResetReconnectionAttempts);
        __instance.ResetReconnectionAttempts();
        __instance.DisableRefreshJoinToken();
        
        SchedulerSystem.AddJob(__instance.RefreshJoinToken, 300f, 0f, 1);
        RichPresence.LastConnectedToServer = DiscordTime.TimeNow();
        if (__instance.GameNetwork.ConnectionState == ConnectionState.Connected)
        {
            using DRMessageHelper drmessageHelper = new(Tags.AuthenticationProfileSelection);
            drmessageHelper.Write(Instances.IsReconnecting); // NOTE: this is broken on GS side
            drmessageHelper.Write(Instances.RejoinToken);
            drmessageHelper.Send(__instance.GameNetwork);
        }
        
        MetaPort.Instance.CurrentInstanceId = Instances.RequestedInstance;
        CohtmlHud.Instance.SetDisplayChain(0);
        CohtmlHud.Instance.ClearViewDropText();
        Instances.ForceDisconnect = false;
        Instances.IsConnectingInitially = false;
        if (Instances.IsReconnecting) CVRGameEventSystem.Instance.OnConnectionRecovered.Invoke(MetaPort.Instance.CurrentInstanceId);
        else if (Comms_SettingsHandler.MuteInputOnJoin) Comms_Manager.IsMicMuted = true;
        Instances.IsReconnecting = false;
        return false;
    }

    private static void CheckIfConnectionIsStable()
    {
        ReconnectionSystemFix.Logger.Msg("Checking if connection is stable...");
        
        if (Time.time - LatestReconnectTime > 3f)
        {
            ReconnectionSystemFix.Logger.Msg("Connection is stable!");
            IsRecoveringFromReconnectionEvent = false;
            SchedulerSystem.RemoveJob(CheckIfConnectionIsStable);
        }
    }
}

internal static class AvatarUpdatePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AvatarUpdate), nameof(AvatarUpdate.Apply))]
    private static bool Prefix_AvatarUpdate_Apply(Message message)
    {
        if (!NetworkManagerPatches.IsRecoveringFromReconnectionEvent) 
            return true; // normal operation
        
        // hack 2
        using DRMessageHelper drmessageHelper = new(message);
        drmessageHelper.Read(out string _);
        drmessageHelper.Read(out string avatarId);
        return CVRPlayerManager.Instance.NetworkPlayers.All(player => player.AvatarId != avatarId);
    }
}

internal static class CVRPlayerManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPlayerManager), nameof(CVRPlayerManager.TryCreatePlayer))]
    private static bool Prefix_CVRPlayerManager_TryCreatePlayer(Message message)
    {
        if (!NetworkManagerPatches.IsRecoveringFromReconnectionEvent) 
            return true; // normal operation

        // hack
        using DRMessageHelper drmessageHelper = new(message);
        drmessageHelper.Read(out string userId);
        return CVRPlayerManager.Instance.NetworkPlayers.All(player => player.Uuid != userId);
    }
}