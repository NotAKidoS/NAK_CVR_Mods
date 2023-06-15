using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.SmoothRay.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.vrLeftHandTracker.gameObject.AddComponent<SmoothRayer>().ray = __instance.leftRay;
        __instance.vrRightHandTracker.gameObject.AddComponent<SmoothRayer>().ray = __instance.rightRay;
    }
}