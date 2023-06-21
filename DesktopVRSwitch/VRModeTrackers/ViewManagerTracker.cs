using ABI_RC.Core.InteractionSystem;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class ViewManagerTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPreVRModeSwitch += OnPreSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPreVRModeSwitch -= OnPreSwitch;
    }

    public void OnPreSwitch(bool intoVR)
    {
        DesktopVRSwitch.Logger.Msg("Closing ViewManager - Main Menu.");

        ViewManager.Instance.UiStateToggle(false);
    }
}