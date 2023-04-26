using ABI_RC.Core.Player;
using HarmonyLib;
using Valve.VR;

namespace NAK.TrackedControllerFix.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        // Add TrackedControllerFix
        var vrLeftHandTracker = __instance.vrLeftHandTracker.AddComponent<TrackedControllerFixer>();
        vrLeftHandTracker.inputSource = SteamVR_Input_Sources.LeftHand;
        var vrRightHandTracker = __instance.vrRightHandTracker.AddComponent<TrackedControllerFixer>();
        vrRightHandTracker.inputSource = SteamVR_Input_Sources.RightHand;
    }
}