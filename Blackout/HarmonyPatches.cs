using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;

namespace NAK.Blackout.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    //Support for changing VRMode during runtime.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "CalibrateAvatar")]
    private static void CheckVRModeOnSwitch()
    {
        if (Blackout.inVR != MetaPort.Instance.isUsingVr)
        {
            MelonLogger.Msg("VRMode change detected! Reinitializing Blackout Instance...");
            Blackout.inVR = MetaPort.Instance.isUsingVr;
            BlackoutController.Instance.SetupBlackoutInstance();
            BlackoutController.Instance.ChangeBlackoutState(BlackoutController.BlackoutState.Awake);
        }
    }
}