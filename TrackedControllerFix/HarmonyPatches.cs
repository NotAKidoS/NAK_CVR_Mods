using ABI_RC.Core.Player;
using HarmonyLib;
using Valve.VR;

namespace NAK.TrackedControllerFix.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        // Add TrackedControllerFix
        var vrLeftHandTracker = __instance.vrLeftHandTracker.AddComponent<TrackedControllerFixer>();
        vrLeftHandTracker.inputSource = SteamVR_Input_Sources.LeftHand;
        var vrRightHandTracker = __instance.vrRightHandTracker.AddComponent<TrackedControllerFixer>();
        vrRightHandTracker.inputSource = SteamVR_Input_Sources.RightHand;
        vrLeftHandTracker.Initialize();
        vrRightHandTracker.Initialize();
    }
}