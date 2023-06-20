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
        ViewManager _viewManager = ViewManager.Instance;
        if (_viewManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting ViewManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Closing ViewManager - Main Menu.");

        _viewManager.UiStateToggle(false);
    }
}