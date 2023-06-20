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
        VRTrackerManager _vrTrackerManager = VRTrackerManager.Instance;
        if (_vrTrackerManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting VRTrackerManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg($"Resetting VRTrackerManager.");

        _vrTrackerManager.poses = null;
        _vrTrackerManager.leftHand = null;
        _vrTrackerManager.rightHand = null;
        _vrTrackerManager.hasCheckedForKnuckles = false;
    }
}