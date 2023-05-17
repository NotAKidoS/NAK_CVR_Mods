using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.IK.TrackingModules;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class IKSystemTracker : MonoBehaviour
{
    public IKSystem ikSystem;

    void Start()
    {
        ikSystem = GetComponent<IKSystem>();
        XRModeSwitchTracker.OnPreXRModeSwitch += PreXRModeSwitch;
        XRModeSwitchTracker.OnFailXRModeSwitch += FailedXRModeSwitch;
        XRModeSwitchTracker.OnPostXRModeSwitch += PostXRModeSwitch;
    }
    void OnDestroy()
    {
        XRModeSwitchTracker.OnPreXRModeSwitch -= PreXRModeSwitch;
        XRModeSwitchTracker.OnFailXRModeSwitch -= FailedXRModeSwitch;
        XRModeSwitchTracker.OnPostXRModeSwitch -= PostXRModeSwitch;
    }

    public void PreXRModeSwitch(bool inXR, Camera activeCamera)
    {
        BodySystem.TrackingEnabled = false;
        BodySystem.TrackingPositionWeight = 0f;
        BodySystem.TrackingLocomotionEnabled = false;
        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = false;
    }

    public void FailedXRModeSwitch(bool inXR, Camera activeCamera)
    {
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.TrackingLocomotionEnabled = true;
        if (IKSystem.vrik != null)
            IKSystem.vrik.enabled = true;
    }

    public void PostXRModeSwitch(bool inXR, Camera activeCamera)
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
        IKSystem.firstAvatarLoaded = DesktopXRSwitch.EntryEnterCalibrationOnSwitch.Value;
        //turn of finger tracking just in case user switched controllers
        ikSystem.FingerSystem.controlActive = false;

        //vrik should be deleted by avatar switch

        SetupOpenXRTrackingModule(inXR);
    }

    void SetupOpenXRTrackingModule(bool enableVR)
    {
        var openXRTrackingModule = ikSystem._trackingModules.OfType<OpenXRTrackingModule>().FirstOrDefault();

        if (openXRTrackingModule != null)
        {
            if (enableVR)
            {
                openXRTrackingModule.ModuleStart();
            }
            else
            {
                openXRTrackingModule.ModuleDestroy();
                if (openXRTrackingModule != null)
                ikSystem._trackingModules.Remove(openXRTrackingModule);
            }
        }
        else if (enableVR)
        {
            var newVRModule = new OpenXRTrackingModule();
            ikSystem.AddTrackingModule(newVRModule);
        }
    }
}