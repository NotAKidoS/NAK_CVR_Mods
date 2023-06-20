using ABI_RC.Core.Savior;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRInputManagerTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    void OnPostSwitch(bool intoVR)
    {
        CVRInputManager _cvrInputManager = CVRInputManager.Instance;
        if (_cvrInputManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVRInputManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Resetting CVRInputManager inputs.");

        _cvrInputManager.inputEnabled = true;

        //just in case
        _cvrInputManager.blockedByUi = false;
        //sometimes head can get stuck, so just in case
        _cvrInputManager.independentHeadToggle = false;
        //just nice to load into desktop with idle gesture
        _cvrInputManager.gestureLeft = 0f;
        _cvrInputManager.gestureLeftRaw = 0f;
        _cvrInputManager.gestureRight = 0f;
        _cvrInputManager.gestureRightRaw = 0f;
        //turn off finger tracking input
        _cvrInputManager.individualFingerTracking = false;
    }
}