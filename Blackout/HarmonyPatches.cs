using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;

namespace NAK.Blackout.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    //Support for changing VRMode during runtime.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.CalibrateAvatar))]
    private static void CheckVRModeOnSwitch()
    {
        if (Blackout.inVR != MetaPort.Instance.isUsingVr)
        {
            Blackout.Logger.Msg("VRMode change detected! Reinitializing Blackout Instance...");
            Blackout.inVR = MetaPort.Instance.isUsingVr;
            BlackoutController.Instance.SetupBlackoutInstance();
            BlackoutController.Instance.ChangeBlackoutState(BlackoutController.BlackoutState.Awake);
        }
    }
}