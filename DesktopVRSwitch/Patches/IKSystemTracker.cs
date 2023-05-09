using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.IK.TrackingModules;
using UnityEngine;

namespace NAK.DesktopVRSwitch.Patches;

public class IKSystemTracker : MonoBehaviour
{
    public IKSystem ikSystem;

    void Start()
    {
        ikSystem = GetComponent<IKSystem>();
        VRModeSwitchTracker.OnPreVRModeSwitch += PreVRModeSwitch;
        VRModeSwitchTracker.OnFailVRModeSwitch += FailedVRModeSwitch;
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }
    void OnDestroy()
    {
        VRModeSwitchTracker.OnPreVRModeSwitch -= PreVRModeSwitch;
        VRModeSwitchTracker.OnFailVRModeSwitch -= FailedVRModeSwitch;
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PreVRModeSwitch(bool enableVR, Camera activeCamera)
    {
        BodySystem.TrackingEnabled = false;
        BodySystem.TrackingPositionWeight = 0f;
        BodySystem.TrackingLocomotionEnabled = false;
        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = false;
    }

    public void FailedVRModeSwitch(bool enableVR, Camera activeCamera)
    {
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.TrackingLocomotionEnabled = true;
        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = true;
    }

    public void PostVRModeSwitch(bool enableVR, Camera activeCamera)
    {
        if (IKSystem.vrik != null)
            DestroyImmediate(IKSystem.vrik);

        //make sure you are fully tracking
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.TrackingLocomotionEnabled = true;
        BodySystem.isCalibratedAsFullBody = false;
        BodySystem.isCalibrating = false;
        BodySystem.isRecalibration = false;
        //make it so you dont instantly end up in FBT from Desktop
        IKSystem.firstAvatarLoaded = DesktopVRSwitch.EntryEnterCalibrationOnSwitch.Value;
        //turn of finger tracking just in case user switched controllers
        ikSystem.FingerSystem.controlActive = false;

        //vrik should be deleted by avatar switch

        SetupSteamVRTrackingModule(enableVR);
    }

    void SetupSteamVRTrackingModule(bool enableVR)
    {
        var openVRModule = ikSystem._trackingModules.OfType<SteamVRTrackingModule>().FirstOrDefault();

        if (openVRModule != null)
        {
            if (enableVR)
            {
                openVRModule.ModuleStart();
            }
            else
            {
                openVRModule.ModuleDestroy();
            }
        }
        else if (enableVR)
        {
            var newVRModule = new SteamVRTrackingModule();
            ikSystem.AddTrackingModule(newVRModule);
        }
    }
}