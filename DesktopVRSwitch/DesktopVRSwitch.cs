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
    public void FailedVRModeSwitch(bool enterVR)
    {
        //let tracked objects know a switch failed
        VRModeSwitchTracker.FailVRModeSwitch(enterVR);
    }

    //one frame before switch attempt
    public void PreVRModeSwitch(bool enterVR)
    {
        //let tracked objects know we are attempting to switch
        VRModeSwitchTracker.PreVRModeSwitch(enterVR);
    }

    //one frame after switch attempt
    public void PostVRModeSwitch(bool enterVR)
    {
        //close the menus
        TryCatchHell.CloseCohtmlMenus();

        //the base of VR checks
        TryCatchHell.SetCheckVR(enterVR);
        TryCatchHell.SetMetaPort(enterVR);

        //game basics for functional gameplay post switch
        TryCatchHell.RepositionCohtmlHud(enterVR);
        TryCatchHell.UpdateHudOperations(enterVR);
        TryCatchHell.DisableMirrorCanvas();
        TryCatchHell.SwitchActiveCameraRigs(enterVR);
        TryCatchHell.ResetCVRInputManager();
        TryCatchHell.UpdateRichPresence();
        TryCatchHell.UpdateGestureReconizerCam();

        //let tracked objects know we switched
        VRModeSwitchTracker.PostVRModeSwitch(enterVR);

        //reload avatar by default, optional for debugging
        if (_reloadLocalAvatar)
        {
            TryCatchHell.ReloadLocalAvatar();
        }

        _switchInProgress = false;
    }
}

