using ABI_RC.Core.Player;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRSwitch.Patches;

public class VRModeSwitchTracker
{
    public static event UnityAction<bool, Camera> OnPreVRModeSwitch;
    public static event UnityAction<bool, Camera> OnPostVRModeSwitch;

    public static void PreVRModeSwitch(bool enterVR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPreVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPreVRModeSwitch?.Invoke(enterVR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPreVRModeSwitch. Did someone do a fucky?");
    }

    public static void PostVRModeSwitch(bool enterVR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPostVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPostVRModeSwitch?.Invoke(enterVR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPostVRModeSwitch. Did someone do a fucky?");
    }
}