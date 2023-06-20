/**

using NAK.DesktopVRSwitch.Patches;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using ABI_RC.Core.Savior;
using ABI_RC.Core;

/**

    SteamVR overrides:
    
    Application.targetFrameRate = -1;
    Application.runInBackground = true;
    QualitySettings.maxQueuedFrames = -1;
    QualitySettings.vSyncCount = 0;
    Time.fixedDeltaTime = Time.timeScale / hmd_DisplayFrequency;

**

namespace NAK.DesktopVRSwitch;

public class DesktopVRSwitcher : MonoBehaviour
{
    //Debug Settings
    public bool _reloadLocalAvatar = true;
    public bool _softVRSwitch = false;

    //Internal Stuff
    private bool _switchInProgress = false;

    void Start()
    {
        //do not pause game, this breaks dynbones & trackers
        SteamVR_Settings.instance.pauseGameWhenDashboardVisible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6) && Input.GetKey(KeyCode.LeftControl))
        {
            SwitchVRMode();
        }
    }

    public void SwitchVRMode()
    {
        if (_switchInProgress) return;
        if (!IsInVR())
        {
            StartCoroutine(StartVRSystem());
        }
        else
        {
            StartCoroutine(StopVR());
        }
    }

    public bool IsInVR() => XRSettings.enabled;

    private IEnumerator StartVRSystem()
    {
        PreVRModeSwitch(true);
        XRSettings.LoadDeviceByName("OpenVR");
        yield return null; //wait a frame before checking

        if (!string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            DesktopVRSwitch.Logger.Msg("Starting SteamVR...");
            XRSettings.enabled = true;
            //force steamvr to reinitialize input
            //this does SteamVR_Input.actionSets[0].Activate() for us (we deactivate in StopVR())
            //but only if SteamVR_Settings.instance.activateFirstActionSetOnStart is enabled
            //which in ChilloutVR, it is, because all those settings are default
            SteamVR_Input.Initialize(true);

            yield return null;

            PostVRModeSwitch(true);
            yield break;
        }

        DesktopVRSwitch.Logger.Error("Initializing VR Failed. Is there no VR device connected?");
        FailedVRModeSwitch(true);
        yield break;
    }

    private IEnumerator StopVR()
    {
        PreVRModeSwitch(false);
        yield return null;

        if (!string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            //SteamVR.SafeDispose(); //might fuck with SteamVRTrackingModule
            //deactivate the action set so SteamVR_Input.Initialize can reactivate
            SteamVR_Input.actionSets[0].Deactivate(SteamVR_Input_Sources.Any);
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;

            yield return null;

            ResetSteamVROverrides();
            PostVRModeSwitch(false);
            yield break;
        }

        DesktopVRSwitch.Logger.Error("Attempted to exit VR without a VR device loaded.");
        FailedVRModeSwitch(false);
        yield break;
    }

    //one frame before switch attempt
    public void PreVRModeSwitch(bool enableVR)
    {
        if (_softVRSwitch) return;
        //let tracked objects know we are attempting to switch
        VRModeSwitchTracker.PreVRModeSwitch(enableVR);
    }

    //one frame after switch attempt
    public void FailedVRModeSwitch(bool enableVR)
    {
        if (_softVRSwitch) return;
        //let tracked objects know a switch failed
        VRModeSwitchTracker.FailVRModeSwitch(enableVR);
    }

    //one frame after switch attempt
    public void PostVRModeSwitch(bool enableVR)
    {
        if (_softVRSwitch) return;

        SetupVR(enableVR);

        _switchInProgress = false;
    }

    public void SetupVR(bool intoVR)
    {
        List<TryCatchHell.TryAction> actions = new List<TryCatchHell.TryAction>
        {
            TryCatchHell.SetCheckVR,
            TryCatchHell.SetMetaPort,
            TryCatchHell.RepositionCohtmlHud,
            TryCatchHell.UpdateHudOperations,
            TryCatchHell.DisableMirrorCanvas,
            TryCatchHell.SwitchActiveCameraRigs,
            TryCatchHell.ResetCVRInputManager,
            TryCatchHell.UpdateRichPresence,
            TryCatchHell.UpdateGestureReconizerCam,
            TryCatchHell.UpdateMenuCoreData,
        };

        foreach (var action in actions)
        {
            TryCatchHell.TryExecute(action, intoVR);
        }

        TryCatchHell.TryExecute(VRModeSwitchTracker.PostVRModeSwitch, intoVR);
    }

    public void ResetSteamVROverrides()
    {
        // Reset physics time to Desktop default
        Time.fixedDeltaTime = 0.02f;

        // Reset queued frames
        QualitySettings.maxQueuedFrames = 2;
        
        // Reset framerate target
        int graphicsFramerateTarget = MetaPort.Instance.settings.GetSettingInt("GraphicsFramerateTarget", 0);
        CVRTools.SetFramerateTarget(graphicsFramerateTarget);

        // Reset VSync setting
        bool graphicsVSync = MetaPort.Instance.settings.GetSettingsBool("GraphicsVSync", false);
        QualitySettings.vSyncCount = graphicsVSync ? 1 : 0;

        // Reset anti-aliasing
        int graphicsMsaaLevel = MetaPort.Instance.settings.GetSettingInt("GraphicsMsaaLevel", 0);
        QualitySettings.antiAliasing = graphicsMsaaLevel;

        // Reset eye tracking initialization
        bool interactionTobiiEyeTracking = MetaPort.Instance.settings.GetSettingsBool("InteractionTobiiEyeTracking", false);
        if (interactionTobiiEyeTracking)
        {
            MetaPort.Instance.TobiiXrInitializer.Initialize();
        }
        else
        {
            // Won't do anything if not already running
            MetaPort.Instance.TobiiXrInitializer.DeInitialize();
        }
    }
}



**/