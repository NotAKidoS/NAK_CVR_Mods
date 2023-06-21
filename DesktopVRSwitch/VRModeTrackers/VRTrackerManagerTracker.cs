using ABI_RC.Core.Player;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class VRTrackerManagerTracker : VRModeTracker
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
        DesktopVRSwitch.Logger.Msg("Resetting VRTrackerManager.");

        // VRTrackerManager will still get old Left/Right hand objects.
        // This only breaks CVRGlobalParams1 reporting battry status
        // MetaPort.Update

        VRTrackerManager.Instance.poses = null;
        VRTrackerManager.Instance.leftHand = null;
        VRTrackerManager.Instance.rightHand = null;
        VRTrackerManager.Instance.hasCheckedForKnuckles = false;
    }
}