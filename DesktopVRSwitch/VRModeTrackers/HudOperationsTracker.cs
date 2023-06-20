using ABI_RC.Core.Player;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class HudOperationsTracker : VRModeTracker
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
        HudOperations _hudOperations = HudOperations.Instance;
        if (_hudOperations == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting HudOperations!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Switching HudOperations worldLoadingItem & worldLoadStatus.");

        _hudOperations.worldLoadingItem = intoVR ? _hudOperations.worldLoadingItemVr : _hudOperations.worldLoadingItemDesktop;
        _hudOperations.worldLoadStatus = intoVR ? _hudOperations.worldLoadStatusVr : _hudOperations.worldLoadStatusDesktop;
    }
}