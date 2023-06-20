using ABI_RC.Core.Savior;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CheckVRTracker : VRModeTracker
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
        CheckVR _checkVR = CheckVR.Instance;
        if (_checkVR == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CheckVR!");
            return;
        }
        DesktopVRSwitch.Logger.Msg($"Setting CheckVR hasVrDeviceLoaded to {intoVR}.");

        _checkVR.hasVrDeviceLoaded = intoVR;
    }
}