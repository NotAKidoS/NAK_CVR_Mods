using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.InputManagement.InputModules;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRInputManagerTracker : VRModeTracker
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
        DesktopVRSwitch.Logger.Msg("Resetting CVRInputManager inputs.");

        CVRInputManager.Instance.inputEnabled = true;
        
        // IM CRYING
        //CVRInputManager.Instance.reload = true;

        //just in case
        CVRInputManager.Instance.textInputFocused = false;
        //sometimes head can get stuck, so just in case
        CVRInputManager.Instance.independentHeadToggle = false;
        //just nice to load into desktop with idle gesture
        CVRInputManager.Instance.gestureLeft = 0f;
        CVRInputManager.Instance.gestureLeftRaw = 0f;
        CVRInputManager.Instance.gestureRight = 0f;
        CVRInputManager.Instance.gestureRightRaw = 0f;
        //turn off finger tracking input
        CVRInputManager.Instance.individualFingerTracking = false;
        
        //add input module if you started in desktop
        if (CVRInputManager._moduleXR == null)
            CVRInputManager.Instance.AddInputModule(CVRInputManager._moduleXR = new CVRInputModule_XR());

        //enable xr input or whatnot
        CVRInputManager._moduleXR.InputEnabled = args.IsUsingVr;
    }
}