using ABI_RC.Core.Player;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRSwitch.Patches;

public class VRModeSwitchTracker
{
    public static event UnityAction<bool, Camera> OnPreVRModeSwitch;
    public static event UnityAction<bool, Camera> OnPostVRModeSwitch;

    public static void PreVRModeSwitch(bool enterXR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPreVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPreVRModeSwitch?.Invoke(enterXR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPreVRModeSwitch. Did someone do a fucky?");
    }

    public static void PostVRModeSwitch(bool enterXR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPostVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPostVRModeSwitch?.Invoke(enterXR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPostVRModeSwitch. Did someone do a fucky?");
    }
}