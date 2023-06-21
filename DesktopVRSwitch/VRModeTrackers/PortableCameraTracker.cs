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
        DesktopVRSwitch.Logger.Msg("Forcing PortableCamera canvas mirroring off.");

        // Tell the game we are in mirror mode so it'll disable it (if enabled)
        PortableCamera.Instance.mode = MirroringMode.Mirror;
        PortableCamera.Instance.ChangeMirroring();
    }
}
