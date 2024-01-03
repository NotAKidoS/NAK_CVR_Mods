using ABI_RC.Core.Player;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class PlayerSetupTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        DesktopVRSwitch.Logger.Msg("Switching active PlayerSetup camera rigs. Updating Desktop camera FOV.");

        PlayerSetup.Instance.desktopCameraRig.SetActive(!args.IsUsingVr);
        PlayerSetup.Instance.vrCameraRig.SetActive(args.IsUsingVr);
    }
}