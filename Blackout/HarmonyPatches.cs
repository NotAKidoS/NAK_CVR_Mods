using HarmonyLib;
using ABI_RC.Core.Player;

namespace Blackout;

[HarmonyPatch]
internal class HarmonyPatches
{
    //Support for changing VRMode during runtime.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "CalibrateAvatar")]
    private static void CheckVRModeOnSwitch()
    {
        if (Blackout.inVR != PlayerSetup.Instance._inVr)
        {
            BlackoutController.Instance.SetupBlackoutInstance();
        }
    }
}