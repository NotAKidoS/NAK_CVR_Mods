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

    private void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        DesktopVRSwitch.Logger.Msg("Switching HudOperations worldLoadingItem & worldLoadStatus.");

        HudOperations.Instance.worldLoadingItem = args.IsUsingVr ? HudOperations.Instance.worldLoadingItemVr : HudOperations.Instance.worldLoadingItemDesktop;
        HudOperations.Instance.worldLoadStatus = args.IsUsingVr ? HudOperations.Instance.worldLoadStatusVr : HudOperations.Instance.worldLoadStatusDesktop;
    }
}