using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.Jobs;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using HarmonyLib;
using NAK.RelativeSync.Components;
using NAK.RelativeSync.Networking;

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
    private static bool _allowUpdate;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.Update))]
    private static bool Prefix_CVRSpawnable_Update()
    {
        return _allowUpdate;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.FixedUpdate))]
    private static void Postfix_CVRSpawnable_FixedUpdate(ref CVRSpawnable __instance)
    {
        _allowUpdate = true;
        __instance.Update();
        _allowUpdate = false;
    }
}