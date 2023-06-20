using ABI_RC.Systems.Camera;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class PortableCameraTracker : VRModeTracker
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
        PortableCamera _portableCamera = PortableCamera.Instance;
        if (_portableCamera == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting PortableCamera!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Forcing PortableCamera canvas mirroring off.");

        // Tell the game we are in mirror mode so it'll disable it (if enabled)
        _portableCamera.mode = MirroringMode.Mirror;
        _portableCamera.ChangeMirroring();
    }
}
