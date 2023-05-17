using ABI_RC.Core.Player;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class XRModeSwitchTracker
{
    public static event UnityAction<bool, Camera> OnPreXRModeSwitch;
    public static event UnityAction<bool, Camera> OnPostXRModeSwitch;
    public static event UnityAction<bool, Camera> OnFailXRModeSwitch;

    public static void PreXRModeSwitch(bool isXR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Invoking XRModeSwitchTracker.OnPreXRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            XRModeSwitchTracker.OnPreXRModeSwitch?.Invoke(isXR, activeCamera);
        },
        "Error while invoking XRModeSwitchTracker.OnPreXRModeSwitch. Did someone do a fucky?");
    }

    public static void PostXRModeSwitch(bool isXR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Invoking XRModeSwitchTracker.OnPostXRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            XRModeSwitchTracker.OnPostXRModeSwitch?.Invoke(isXR, activeCamera);
        },
        "Error while invoking XRModeSwitchTracker.OnPostXRModeSwitch. Did someone do a fucky?");
    }

    public static void FailXRModeSwitch(bool isXR)
    {
        TryCatchHell.TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Invoking XRModeSwitchTracker.OnFailXRModeSwitch.");
            Camera activeCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            XRModeSwitchTracker.OnFailXRModeSwitch?.Invoke(isXR, activeCamera);
        },
        "Error while invoking OnFailXRModeSwitch.OnPreXRModeSwitch. Did someone do a fucky?");
    }
}