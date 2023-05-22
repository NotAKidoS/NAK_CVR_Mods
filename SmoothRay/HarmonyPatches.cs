using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.SmoothRay.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        var leftSmoother = __instance.vrLeftHandTracker.gameObject.AddComponent<SmoothRayer>();
        leftSmoother.ray = __instance.leftRay;
        var rightSmoother = __instance.vrRightHandTracker.gameObject.AddComponent<SmoothRayer>();
        rightSmoother.ray = __instance.rightRay;
    }
}