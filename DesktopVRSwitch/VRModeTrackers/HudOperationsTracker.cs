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
        DesktopVRSwitch.Logger.Msg("Switching HudOperations worldLoadingItem & worldLoadStatus.");

        HudOperations.Instance.worldLoadingItem = intoVR ? HudOperations.Instance.worldLoadingItemVr : HudOperations.Instance.worldLoadingItemDesktop;
        HudOperations.Instance.worldLoadStatus = intoVR ? HudOperations.Instance.worldLoadStatusVr : HudOperations.Instance.worldLoadStatusDesktop;
    }
}