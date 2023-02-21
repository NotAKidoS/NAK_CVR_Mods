using NAK.Melons.DesktopVRSwitch.Patches;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace NAK.Melons.DesktopVRSwitch;

public class DesktopVRSwitch : MonoBehaviour
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
            DesktopVRSwitchMod.Logger.Msg("Starting SteamVR...");
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
        DesktopVRSwitchMod.Logger.Error("Initializing VR Failed. Is there no VR device connected?");
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
            Time.fixedDeltaTime = 0.02f; //reset physics time to Desktop default
            PostVRModeSwitch(false);
            yield break;
        }
        DesktopVRSwitchMod.Logger.Error("Attempted to exit VR without a VR device loaded.");
        FailedVRModeSwitch(true);
        yield break;
    }

    //one frame after switch attempt
    public void FailedVRModeSwitch(bool isVR)
    {
        if (_softVRSwitch) return;
        //let tracked objects know a switch failed
        VRModeSwitchTracker.FailVRModeSwitch(isVR);
    }

    //one frame before switch attempt
    public void PreVRModeSwitch(bool isVR)
    {
        if (_softVRSwitch) return;
        //let tracked objects know we are attempting to switch
        VRModeSwitchTracker.PreVRModeSwitch(isVR);
    }

    //one frame after switch attempt
    public void PostVRModeSwitch(bool isVR)
    {
        if (_softVRSwitch) return;
        //close the menus
        TryCatchHell.CloseCohtmlMenus();

        //the base of VR checks
        TryCatchHell.SetCheckVR(isVR);
        TryCatchHell.SetMetaPort(isVR);

        //game basics for functional gameplay post switch
        TryCatchHell.RepositionCohtmlHud(isVR);
        TryCatchHell.UpdateHudOperations(isVR);
        TryCatchHell.DisableMirrorCanvas();
        TryCatchHell.SwitchActiveCameraRigs(isVR);
        TryCatchHell.ResetCVRInputManager();
        TryCatchHell.UpdateRichPresence();
        TryCatchHell.UpdateGestureReconizerCam();
        TryCatchHell.UpdateMenuCoreData(isVR);

        //let tracked objects know we switched
        VRModeSwitchTracker.PostVRModeSwitch(isVR);

        //reload avatar by default, optional for debugging
        if (_reloadLocalAvatar)
        {
            TryCatchHell.ReloadLocalAvatar();
        }

        _switchInProgress = false;
    }
}

