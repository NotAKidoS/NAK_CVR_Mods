using ABI_RC.Core.InteractionSystem;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVR_MenuManagerTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPreVRModeSwitch += OnPreSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPreVRModeSwitch -= OnPreSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPreSwitch(bool intoVR)
    {
        CVR_MenuManager _cvrMenuManager = CVR_MenuManager.Instance;
        if (_cvrMenuManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVR_MenuManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Closing CVR_MenuManager - Quick Menu.");

        _cvrMenuManager.ToggleQuickMenu(false);
    }

    private void OnPostSwitch(bool intoVR)
    {
        CVR_MenuManager _cvrMenuManager = CVR_MenuManager.Instance;
        if (_cvrMenuManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVR_MenuManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Updating CVR_Menu_Data core data.");

        _cvrMenuManager.coreData.core.inVr = intoVR;
    }
}