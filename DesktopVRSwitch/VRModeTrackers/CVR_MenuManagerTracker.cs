using ABI_RC.Core.InteractionSystem;
using UnityEngine;

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
        DesktopVRSwitch.Logger.Msg("Closing CVR_MenuManager - Quick Menu.");

        CVR_MenuManager.Instance.ToggleQuickMenu(false);
    }

    private void OnPostSwitch(bool intoVR)
    {
        DesktopVRSwitch.Logger.Msg("Updating CVR_Menu_Data core data.");

        CVR_MenuManager.Instance.coreData.core.inVr = intoVR;
        CVR_MenuManager.Instance.quickMenu.transform.localPosition = Vector3.zero;
        CVR_MenuManager.Instance.quickMenu.transform.localRotation = Quaternion.identity;
    }
}