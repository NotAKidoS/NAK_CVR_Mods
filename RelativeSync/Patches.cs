using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.Jobs;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using HarmonyLib;
using NAK.RelativeSync.Components;
using NAK.RelativeSync.Networking;
using UnityEngine;

namespace NAK.RelativeSync.Patches;

internal static class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMonitor>();
    }
}

internal static class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.Start))]
    private static void Postfix_PuppetMaster_Start(ref PuppetMaster __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncController>();
    }
}

internal static class CVRSeatPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSeat), nameof(CVRSeat.Awake))]
    private static void Postfix_CVRSeat_Awake(ref CVRSeat __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMarker>();
    }
}

internal static class CVRMovementParentPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRMovementParent), nameof(CVRMovementParent.Start))]
    private static void Postfix_CVRMovementParent_Start(ref CVRMovementParent __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMarker>();
    }
}

internal static class NetworkRootDataUpdatePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkRootDataUpdate), nameof(NetworkRootDataUpdate.Submit))]
    private static void Postfix_NetworkRootDataUpdater_Submit()
    {
        ModNetwork.SendRelativeSyncUpdate(); // Send the relative sync update after the network root data update
    }
}

internal static class CVRSpawnablePatches
{
    internal static bool UseHack;
    
    private static bool _canUpdate;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.Update))]
    private static bool Prefix_CVRSpawnable_Update()
        => !UseHack || _canUpdate;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.FixedUpdate))]
    private static void Postfix_CVRSpawnable_FixedUpdate(ref CVRSpawnable __instance)
    {
        if (!UseHack) return;
        
        _canUpdate = true;
        __instance.Update();
        _canUpdate = false;
    }
}

internal static class NetIKController_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetIKController), nameof(NetIKController.LateUpdate))]
    private static void Postfix_NetIKController_LateUpdate(ref NetIKController __instance)
    {
        if (!RelativeSyncManager.NetIkControllersToRelativeSyncControllers.TryGetValue(__instance,
                out RelativeSyncController syncController))
            return;
        
        // Apply relative sync after the network IK has been applied
        syncController.OnPostNetIkControllerLateUpdate();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetIKController), nameof(NetIKController.GetLocalPlayerPosition))]
    private static bool Prefix_NetIKController_GetLocalPlayerPosition(ref NetIKController __instance, ref Vector3 __result)
    {
        // why is the original method so bad
        __result = PlayerSetup.Instance.activeCam.transform.position;
        return false;
    }
}