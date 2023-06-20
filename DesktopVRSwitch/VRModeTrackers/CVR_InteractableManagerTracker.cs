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

    private void OnPostSwitch(bool intoVR)
    {
        CVR_InteractableManager _cvrInteractableManager = CVR_InteractableManager.Instance;
        if (_cvrInteractableManager == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVR_InteractableManager!");
            return;
        }
        DesktopVRSwitch.Logger.Msg($"Setting CVRInputManager inputEnabled & CVR_InteractableManager enableInteractions to {!intoVR}");

        CVR_InteractableManager.enableInteractions = !intoVR;
    }
}