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
        var leftFixer = __instance.vrLeftHandTracker.AddComponent<TrackedControllerFixer>();
        leftFixer.inputSource = SteamVR_Input_Sources.LeftHand;
        leftFixer.Initialize();
        var rightFixer = __instance.vrRightHandTracker.AddComponent<TrackedControllerFixer>();
        rightFixer.inputSource = SteamVR_Input_Sources.RightHand;
        rightFixer.Initialize();
    }
}