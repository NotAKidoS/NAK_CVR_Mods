using ABI_RC.Core.Player;
using HarmonyLib;
using Valve.VR;

namespace NAK.Melons.TrackedControllerFix.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        // Add TrackedControllerFix
        var vrLeftHandTracker = __instance.vrLeftHandTracker.AddComponent<TrackedControllerFix>();
        vrLeftHandTracker.inputSource = SteamVR_Input_Sources.LeftHand;
        var vrRightHandTracker = __instance.vrRightHandTracker.AddComponent<TrackedControllerFix>();
        vrRightHandTracker.inputSource = SteamVR_Input_Sources.RightHand;
    }
}