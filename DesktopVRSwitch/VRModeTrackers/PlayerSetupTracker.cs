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

        // This might error if we started in VR.
        // '_cam' is not set until Start().
        if (CVR_DesktopCameraController._cam == null)
            CVR_DesktopCameraController._cam = _playerSetup.desktopCamera.GetComponent<UnityEngine.Camera>();

        CVR_DesktopCameraController.UpdateFov();

        // UICamera has a script that copies the FOV from the desktop cam.
        // Toggling the cameras on/off resets the aspect ratio,
        // so when rigs switch, that is already handled.
    }
}