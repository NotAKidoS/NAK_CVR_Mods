using ABI_RC.Core;
using ABI_RC.Core.UI;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CohtmlHudTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        DesktopVRSwitch.Logger.Msg("Configuring new hud affinity for CohtmlHud.");

        CohtmlHud.Instance.gameObject.transform.parent = Utils.GetPlayerCameraObject(args.IsUsingVr).transform;
        
        // This handles rotation and position
        CVRTools.ConfigureHudAffinity();
        CohtmlHud.Instance.gameObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        
        // required to set menu vr mode (why is it offset in js?)
        CohtmlHud.uiCoreGameData.isVr = args.IsUsingVr;
        if (CohtmlHud.Instance._isReady)
            CohtmlHud.Instance.hudView.View.TriggerEvent("updateCoreGameVars", CohtmlHud.uiCoreGameData);
    }
}