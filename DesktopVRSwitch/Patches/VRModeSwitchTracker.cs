using ABI_RC.Core.Player;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRSwitch.Patches;

public class VRModeSwitchTracker
{
    public static event UnityAction<bool, Camera> OnPreVRModeSwitch;
    public static event UnityAction<bool, Camera> OnPostVRModeSwitch;
    public static event UnityAction<bool, Camera> OnFailVRModeSwitch;

    public static void PreVRModeSwitch(bool isVR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPreVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPreVRModeSwitch?.Invoke(isVR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPreVRModeSwitch. Did someone do a fucky?");
    }

    public static void PostVRModeSwitch(bool isVR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnPostVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnPostVRModeSwitch?.Invoke(isVR, activeCamera);
        },
        "Error while invoking VRModeSwitchTracker.OnPostVRModeSwitch. Did someone do a fucky?");
    }

    public static void FailVRModeSwitch(bool isVR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopVRSwitchMod.Logger.Msg("Invoking VRModeSwitchTracker.OnFailVRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            VRModeSwitchTracker.OnFailVRModeSwitch?.Invoke(isVR, activeCamera);
        },
        "Error while invoking OnFailVRModeSwitch.OnPreVRModeSwitch. Did someone do a fucky?");
    }
}