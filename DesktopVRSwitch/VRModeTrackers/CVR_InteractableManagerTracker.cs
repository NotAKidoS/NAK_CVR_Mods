using ABI_RC.Core.InteractionSystem;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVR_InteractableManagerTracker : VRModeTracker
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
        DesktopVRSwitch.Logger.Msg($"Setting CVRInputManager inputEnabled & CVR_InteractableManager enableInteractions to {!args.IsUsingVr}");

        CVR_InteractableManager.enableInteractions = !args.IsUsingVr;
    }
}