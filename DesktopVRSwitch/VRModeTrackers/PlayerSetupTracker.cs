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

    private void OnPostSwitch(bool intoVR)
    {
        PlayerSetup _playerSetup = PlayerSetup.Instance;
        if (_playerSetup == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting PlayerSetup!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Switching active PlayerSetup camera rigs. Updating Desktop camera FOV.");

        _playerSetup.desktopCameraRig.SetActive(!intoVR);
        _playerSetup.vrCameraRig.SetActive(intoVR);
    }
}