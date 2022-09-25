using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.MovementSystem;
using MelonLoader;
using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    private static System.Object melon;
    private static bool isAttemptingSwitch = false;
    private static float timedSwitch = 0f;

    public override void OnUpdate()
    {
        // assuming CVRInputManager.switchMode button was originally for desktop/vr switching before being left to do literally nothing in rootlogic
        if (Input.GetKeyDown(KeyCode.F6) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && !isAttemptingSwitch)
        {
            //start attempt
            isAttemptingSwitch = true;
            melon = MelonCoroutines.Start(AttemptPlatformSwitch());
            timedSwitch = Time.time + 10f;
        }

        if (isAttemptingSwitch && Time.time > timedSwitch)
        {
            isAttemptingSwitch = false;
            MelonCoroutines.Stop(melon);
            MelonLogger.Msg("Timer exceeded. Something is wrong.");
        }
    }

    private static IEnumerator AttemptPlatformSwitch()
    {
        bool toVR = !MetaPort.Instance.isUsingVr;

        //load SteamVR/OpenVR if entering VR
        MelonCoroutines.Start(LoadDevice("OpenVR", toVR));

        //we need to wait a frame or meet doom :shock: :shock: :stare:
        //we are waiting a frame in LoadDevice after LoadDeviceByName()
        yield return new WaitForEndOfFrame();

        CloseMenuElements(toVR);

        yield return new WaitForEndOfFrame();

        SetMetaPort(toVR);

        yield return new WaitForEndOfFrame();

        SetPlayerSetup(toVR);

        yield return new WaitForEndOfFrame();

        SetMovementSystem(toVR);

        yield return new WaitForEndOfFrame();

        SetSteamVRInstances(toVR);

        yield return new WaitForEndOfFrame();

        ReloadCVRInputManager();

        //some menus have 0.5s wait(), so to be safe
        yield return new WaitForSeconds(1f);

        Recalibrate();

        yield return null;
        isAttemptingSwitch = false;
    }
    private static IEnumerator LoadDevice(string newDevice, bool isVR)
    {
        if (isVR)
        {
            if (String.Compare(XRSettings.loadedDeviceName, newDevice, true) != 0)
            {
                XRSettings.LoadDeviceByName(newDevice);
                yield return null;
                XRSettings.enabled = true;
                SteamVR.settings.pauseGameWhenDashboardVisible = false;
                if (SteamVR_Behaviour.instance.enabled == false)
                {
                    SteamVR_Behaviour.instance.enabled = true;
                    SteamVR_Render.instance.enabled = true;
                }
            }
            else
            {
                MelonLogger.Msg("OpenVR device already loaded!");
                MelonCoroutines.Stop(melon);
                yield return null;
                XRSettings.enabled = true;
                if (SteamVR_Behaviour.instance.enabled == false)
                {
                    SteamVR_Behaviour.instance.enabled = true;
                    SteamVR_Render.instance.enabled = true;
                }
                isAttemptingSwitch = false;
            }
        }
        else
        {
            //holyfuck that was a lot of trial and error
            SteamVR.enabled = false;
            yield return new WaitForEndOfFrame();
            XRSettings.LoadDeviceByName("None");
            yield return null;
        }
    }

    // shouldn't be that important, right?
    private static void CloseMenuElements(bool isVR)
    {
        if (ViewManager.Instance != null)
        {
            MelonLogger.Msg("Closed MainMenu Instance.");
            ViewManager.Instance.UiStateToggle(false);
            ViewManager.Instance.VrInputChanged(isVR);
        }
        else
        {
            MelonLogger.Msg("MainMenu Instance not found!!!");
        }
        if (ViewManager.Instance != null)
        {
            MelonLogger.Msg("Closed QuickMenu Instance.");
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
        }
        else
        {
            MelonLogger.Msg("QuickMenu Instance not found!!!");
        }
    }

    private static void SetMetaPort(bool isVR)
    {
        if (MetaPort.Instance == null)
        {
            MelonLogger.Msg("MetaPort Instance not found!!!");
            return;
        }
        MelonLogger.Msg($"Set MetaPort isUsingVr to {isVR}.");
        MetaPort.Instance.isUsingVr = isVR;
    }

    //uh huh
    private static void SetSteamVRInstances(bool isVR)
    {
        if (SteamVR_Behaviour.instance == null)
        {
            MelonLogger.Msg("SteamVR Instances not found!!!");
            return;
        }
        MelonLogger.Msg($"Set SteamVR monobehavior instances to {isVR}.");
        SteamVR_Behaviour.instance.enabled = isVR;
        SteamVR_Render.instance.enabled = isVR;
        //set again just in case on desktop & disabling
        XRSettings.enabled = isVR;
    }

    private static void SetPlayerSetup(bool isVR)
    {
        if (PlayerSetup.Instance == null)
        {
            MelonLogger.Msg("PlayerSetup Instance not found!!!");
            return;
        }

        if (isVR)
        {
            MelonLogger.Msg("Creating temp VRIK component.");
            VRIK ik = (VRIK)PlayerSetup.Instance._avatar.GetComponent(typeof(VRIK));
            if (ik == null)
            {
                ik = PlayerSetup.Instance._avatar.AddComponent<VRIK>();
            }
            ik.solver.IKPositionWeight = 0f;
            ik.enabled = false;
        }

        MelonLogger.Msg($"Set PlayerSetup instance to {isVR}.");
        PlayerSetup.Instance._inVr = isVR;

        //we invoke calibrate to get VRIK and calibrator instance set up, faster than full recalibrate
        MelonLogger.Msg("Called CalibrateAvatar() on PlayerSetup.Instance. Expect a few errors from PlayerSetup Update() and LateUpdate().");
        PlayerSetup.Instance.CalibrateAvatar();

        MelonLogger.Msg("Switched active camera rigs.");
        PlayerSetup.Instance.desktopCameraRig.SetActive(!isVR);
        PlayerSetup.Instance.vrCameraRig.SetActive(isVR);

        if (CohtmlHud.Instance == null)
        {
            MelonLogger.Msg("CohtmlHud Instance not found!!!");
            return;
        }
        MelonLogger.Msg("Parented CohtmlHud to active camera.");
        CohtmlHud.Instance.gameObject.transform.parent = isVR ? PlayerSetup.Instance.vrCamera.transform : PlayerSetup.Instance.desktopCamera.transform;

        //i think the VR offset depends on headset... cant find where in the games code it is though so could be wrong... ?
        CohtmlHud.Instance.gameObject.transform.localPosition = isVR ? new Vector3(-0.2f, -0.391f, 1.244f) : new Vector3(0f, 0f, 1.3f);
        CohtmlHud.Instance.gameObject.transform.localRotation = Quaternion.Euler( new Vector3(0f, 180f, 0f) );
    }

    //hopefully whatever rework was hinted at doesn't immediatly break this
    private static void SetMovementSystem(bool isVR)
    {
        if (MovementSystem.Instance == null)
        {
            MelonLogger.Msg("MovementSystem Instance not found!!!");
            return;
        }
        MelonLogger.Msg($"Set MovementSystem instance to {isVR}.");
        MovementSystem.Instance.isVr = true;
    }

    private static void ReloadCVRInputManager()
    {
        if (CVRInputManager.Instance == null)
        {
            MelonLogger.Msg("CVRInputManager Instance not found!!!");
            return;
        }
        MelonLogger.Msg("Set CVRInputManager reload to True. Input should reload next frame...");
        CVRInputManager.Instance.reload = true;
        CVRInputManager.Instance.inputEnabled = true;
        CVRInputManager.Instance.blockedByUi = false;
        CVRInputManager.Instance.independentHeadToggle = false;
        CVRInputManager.Instance.gestureLeft = 0f;
        CVRInputManager.Instance.gestureLeftRaw = 0f;
        CVRInputManager.Instance.gestureRight = 0f;
        CVRInputManager.Instance.gestureRightRaw = 0f;
    }

    private static void Recalibrate()
    {
        if (PlayerSetup.Instance == null)
        {
            MelonLogger.Msg("PlayerSetup Instance not found!!!");
            return;
        }
        MelonLogger.Msg("Called ReCalibrateAvatar() on PlayerSetup.Instance. Will take a second...");
        PlayerSetup.Instance.ReCalibrateAvatar();
    }
}