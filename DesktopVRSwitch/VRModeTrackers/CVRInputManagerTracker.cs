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
        DesktopVRSwitch.Logger.Msg("Resetting CVRInputManager inputs.");

        CVRInputManager.Instance.inputEnabled = true;

        //just in case
        CVRInputManager.Instance.blockedByUi = false;
        //sometimes head can get stuck, so just in case
        CVRInputManager.Instance.independentHeadToggle = false;
        //just nice to load into desktop with idle gesture
        CVRInputManager.Instance.gestureLeft = 0f;
        CVRInputManager.Instance.gestureLeftRaw = 0f;
        CVRInputManager.Instance.gestureRight = 0f;
        CVRInputManager.Instance.gestureRightRaw = 0f;
        //turn off finger tracking input
        CVRInputManager.Instance.individualFingerTracking = false;
    }
}