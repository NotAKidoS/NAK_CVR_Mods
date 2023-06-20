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

    private void OnPostSwitch(bool intoVR)
    {
        CohtmlHud _cohtmlHud = CohtmlHud.Instance;
        if (_cohtmlHud == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CohtmlHud!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Configuring new hud affinity for CohtmlHud.");

        _cohtmlHud.gameObject.transform.parent = Utils.GetPlayerCameraObject(intoVR).transform;
        // This handles rotation and position
        CVRTools.ConfigureHudAffinity();
        _cohtmlHud.gameObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
    }
}