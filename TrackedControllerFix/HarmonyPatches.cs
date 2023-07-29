using ABI_RC.Core.Player;
using HarmonyLib;
using Valve.VR;

namespace NAK.TrackedControllerFix.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.vrLeftHandTracker.AddComponent<TrackedControllerFixer>().inputSource = SteamVR_Input_Sources.LeftHand;
        __instance.vrRightHandTracker.AddComponent<TrackedControllerFixer>().inputSource = SteamVR_Input_Sources.RightHand;
    }
}