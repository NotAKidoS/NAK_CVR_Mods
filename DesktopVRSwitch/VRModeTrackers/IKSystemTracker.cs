using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.IK.TrackingModules;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class IKSystemTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPreVRModeSwitch += OnPreSwitch;
        VRModeSwitchManager.OnFailVRModeSwitch += OnFailedSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPreVRModeSwitch -= OnPreSwitch;
        VRModeSwitchManager.OnFailVRModeSwitch -= OnFailedSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPreSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        BodySystem.TrackingEnabled = false;
        BodySystem.TrackingPositionWeight = 0f;
        BodySystem.TrackingLocomotionEnabled = false;

        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = false;
    }

    private void OnFailedSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.TrackingLocomotionEnabled = true;

        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = true;
    }

    private void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        if (IKSystem.vrik != null)
            UnityEngine.Object.DestroyImmediate(IKSystem.vrik);

        // Make sure you are fully tracking
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.TrackingLocomotionEnabled = true;
        BodySystem.isCalibratedAsFullBody = false;
        BodySystem.isCalibrating = false;
        BodySystem.isRecalibration = false;

        // Make it so you don't instantly end up in FBT from Desktop
        IKSystem.firstAvatarLoaded = ModSettings.EntryEnterCalibrationOnSwitch.Value;

        // Turn off finger tracking just in case the user switched controllers
        IKSystem.Instance.FingerSystem.controlActive = false;

        SetupSteamVRTrackingModule(args.IsUsingVr);
    }

    private void SetupSteamVRTrackingModule(bool enableVR)
    {
        var openVRModule = IKSystem.Instance._trackingModules.OfType<SteamVRTrackingModule>().FirstOrDefault();

        if (openVRModule != null)
        {
            if (enableVR)
                openVRModule.ModuleStart();
            else
                openVRModule.ModuleDestroy();
        }
        else if (enableVR)
        {
            var newVRModule = new SteamVRTrackingModule();
            IKSystem.Instance.AddTrackingModule(newVRModule);
        }
    }
}