using ABI.CCK.Components;
using HarmonyLib;

namespace NAK.RelativeSyncJitterFix.Patches;

internal static class CVRSpawnablePatches
{
    private static bool _canUpdate;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.FixedUpdate))]
    private static void Postfix_CVRSpawnable_FixedUpdate(ref CVRSpawnable __instance)
    {
        _canUpdate = true;
        __instance.Update();
        _canUpdate = false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.Update))]
    private static bool Prefix_CVRSpawnable_Update() => _canUpdate;
}