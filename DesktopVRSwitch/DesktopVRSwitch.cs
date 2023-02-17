using NAK.Melons.DesktopVRSwitch.Patches;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace NAK.Melons.DesktopVRSwitch;

public class DesktopVRSwitch : MonoBehaviour
{
    //Settings
    public bool _reloadLocalAvatar = true;

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
            SwitchXRMode();
        }
    }

    public void SwitchXRMode()
    {
        if (_switchInProgress) return;
        if (!IsInXR())
        {
            StartCoroutine(StartXRSystem());
        }
        else
        {
            StartCoroutine(StopXR());
        }
    }

    public bool IsInXR() => XRSettings.enabled;

    private IEnumerator StartXRSystem()
    {
        BeforeXRModeSwitch(true);
        XRSettings.LoadDeviceByName("OpenVR");
        yield return null;
        if (string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            DesktopVRSwitchMod.Logger.Error("Initializing VR Failed. Is there no VR device connected?");
        }
        else
        {
            DesktopVRSwitchMod.Logger.Msg("Starting SteamVR...");
            XRSettings.enabled = true;
            //force steamvr to reinitialize input
            //this does SteamVR_Input.actionSets[0].Activate() for us (we deactivate in StopVR())
            //but only if SteamVR_Settings.instance.activateFirstActionSetOnStart is enabled
            //which in ChilloutVR, it is, because all those settings are default
            SteamVR_Input.Initialize(true);
            yield return null;
            AfterXRModeSwitch(true);
        }
        yield break;
    }

    private IEnumerator StopXR()
    {
        BeforeXRModeSwitch(false);
        yield return null;
        if (!string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            //deactivate the action set so SteamVR_Input.Initialize can reactivate
            SteamVR_Input.actionSets[0].Deactivate(SteamVR_Input_Sources.Any);
            SteamVR.SafeDispose(); //idk
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;
            yield return null;
            AfterXRModeSwitch(false);
        }
        yield break;
    }

    //one frame before switch attempt
    public void BeforeXRModeSwitch(bool enterXR)
    {
        //let tracked objects know we are attempting to switch
        VRModeSwitchTracker.PreVRModeSwitch(enterXR);
    }

    //one frame after switch attempt
    public void AfterXRModeSwitch(bool enterXR)
    {
        //reset physics time to Desktop default
        Time.fixedDeltaTime = 0.02f;

        //these two must come first
        TryCatchHell.SetCheckVR(enterXR);
        TryCatchHell.SetMetaPort(enterXR);

        //the bulk of funni changes
        TryCatchHell.RepositionCohtmlHud(enterXR);
        TryCatchHell.UpdateHudOperations(enterXR);
        TryCatchHell.DisableMirrorCanvas();
        TryCatchHell.SwitchActiveCameraRigs(enterXR);
        TryCatchHell.ResetCVRInputManager();
        TryCatchHell.UpdateRichPresence();
        TryCatchHell.UpdateGestureReconizerCam();

        //let tracked objects know we switched
        VRModeSwitchTracker.PostVRModeSwitch(enterXR);

        //reload avatar by default, optional for debugging
        if (_reloadLocalAvatar)
        {
            TryCatchHell.ReloadLocalAvatar();
        }

        _switchInProgress = false;
    }
}

