using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.MovementSystem;
using MelonLoader;
using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.XR;
using Valve.VR;

namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    private static bool isAttemptingSwitch = false;
    private static float timedSwitch = 0f;

    public override void OnUpdate()
    {
        // assuming CVRInputManager.switchMode button was originally for desktop/vr switching before being left to do literally nothing in rootlogic
        if (Input.GetKeyDown(KeyCode.F6) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && !isAttemptingSwitch)
        {
            //start attempt
            isAttemptingSwitch = true;
            MelonCoroutines.Start(AttemptPlatformSwitch());
            //how long we wait until we assume an error occured
            timedSwitch = Time.time + 10f;
        }

        //catch if coroutine just decided to not finish... which happens?
        if (isAttemptingSwitch && Time.time > timedSwitch)
        {
            isAttemptingSwitch = false;
            MelonLogger.Error("Timer exceeded. Something is wrong and coroutine failed partway.");
        }
    }

    private static IEnumerator AttemptPlatformSwitch()
    {
        bool toVR = !MetaPort.Instance.isUsingVr;

        //load or unload SteamVR
        if (toVR)
        {
            //force SteamVR to fully initialize, this does all and more than what i did with LoadDevice()
            SteamVR.Initialize(true);
            
            //Just to make sure. Game does this natively when entering VR.
            SteamVR_Settings.instance.pauseGameWhenDashboardVisible = false;

            //TODO: something needs to be done to reinitialize SteamVR_Input or SteamVR_Actions
            //If you restart SteamVR after already have been in VRMode, the steamvr action handles break
            //ive tried:
            //SteamVR_Input.Initialize(true)
            //SteamVR_Actions.PreInitialize()
            //Destroying SteamVR_Settings on DesktopMode
            //Destroying SteamVR_Behavior on DesktopMode
            //Destroying SteamVR_Render on DesktopMode
            //Combinations of all of these..
            //Its probably really simple, but I just cannot figure out how.
        }
        else
        {
            //force SteamVR to let go of Chillout
            XRSettings.LoadDeviceByName("None");
            XRSettings.enabled = false;

            //destroy [SteamVR] gameobject as next SteamVR.Initialize creates a new one
            Object.Destroy(SteamVR_Behaviour.instance.gameObject);

            //what even does this do that is actually important?
            SteamVR.SafeDispose();
        }

        CloseMenuElements(toVR);

        yield return new WaitForEndOfFrame();

        SetMetaPort(toVR);

        yield return new WaitForEndOfFrame();

        SetPlayerSetup(toVR);
        SwitchActiveCameraRigs(toVR);
        CreateTempVRIK(toVR);
        QuickCalibrate(toVR);
        RepositionCohtmlHud(toVR);

        yield return new WaitForEndOfFrame();

        SetMovementSystem(toVR);

        yield return new WaitForEndOfFrame();

        //right here is the fucker most likely to break
        ReloadCVRInputManager();

        //some menus have 0.5s wait(), so to be safe
        yield return new WaitForSeconds(1f);

        Recalibrate();

        yield return null;
        isAttemptingSwitch = false;
    }



    //shitton of try catch below



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
        try
        {
            MelonLogger.Msg($"Set MetaPort isUsingVr to {isVR}.");
            MetaPort.Instance.isUsingVr = isVR;
        }
        catch (Exception)
        {
            MelonLogger.Error("Setting MetaPort isUsingVr failed. Is MetaPort.Instance invalid?");
            MelonLogger.Msg("MetaPort.Instance: " + MetaPort.Instance);
            throw;
        }
    }

    private static void SetPlayerSetup(bool isVR)
    {
        try
        {
            MelonLogger.Msg($"Set PlayerSetup instance to {isVR}.");
            PlayerSetup.Instance._inVr = isVR;
        }
        catch (Exception)
        {
            MelonLogger.Error("Setting PlayerSetup _inVr failed. Is PlayerSetup.Instance invalid?");
            MelonLogger.Msg("PlayerSetup.Instance: " + PlayerSetup.Instance);
            throw;
        }
    }

    private static void CreateTempVRIK(bool isVR)
    {
        try
        {
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
            else
            {
                MelonLogger.Msg("Temp VRIK component is not needed. Ignoring.");
            }
        }
        catch (Exception)
        {
            MelonLogger.Error("Temp creation of VRIK on avatar failed. Is PlayerSetup.Instance invalid?");
            MelonLogger.Msg("PlayerSetup.Instance: " + PlayerSetup.Instance);
            throw;
        }
    }

    private static void QuickCalibrate(bool isVR)
    {
        try
        {
            //we invoke calibrate to get VRIK and calibrator instance set up, faster than full recalibrate
            MelonLogger.Msg("Called CalibrateAvatar() on PlayerSetup.Instance. Expect a few errors from PlayerSetup Update() and LateUpdate().");
            PlayerSetup.Instance.CalibrateAvatar();
        }
        catch (Exception)
        {
            MelonLogger.Error("CalibrateAvatar() failed. Is PlayerSetup.Instance invalid?");
            MelonLogger.Msg("PlayerSetup.Instance: " + PlayerSetup.Instance);
            throw;
        }
    }

    private static void SwitchActiveCameraRigs(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Switched active camera rigs.");
            PlayerSetup.Instance.desktopCameraRig.SetActive(!isVR);
            PlayerSetup.Instance.vrCameraRig.SetActive(isVR);
        }
        catch (Exception)
        {
            MelonLogger.Error("Error switching active cameras. Are the camera rigs invalid?");
            MelonLogger.Msg("PlayerSetup.Instance.desktopCameraRig: " + PlayerSetup.Instance.desktopCameraRig);
            MelonLogger.Msg("PlayerSetup.Instance.vrCameraRig: " + PlayerSetup.Instance.vrCameraRig);
            throw;
        }
    }

    private static void RepositionCohtmlHud(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Parented CohtmlHud to active camera.");
            CohtmlHud.Instance.gameObject.transform.parent = isVR ? PlayerSetup.Instance.vrCamera.transform : PlayerSetup.Instance.desktopCamera.transform;
            //i think the VR offset may be different between headsets, but i cannot find where in games code they are set
            CohtmlHud.Instance.gameObject.transform.localPosition = isVR ? new Vector3(-0.2f, -0.391f, 1.244f) : new Vector3(0f, 0f, 1.3f);
            CohtmlHud.Instance.gameObject.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
        }
        catch (Exception)
        {
            MelonLogger.Error("Error parenting CohtmlHud to active camera. Is CohtmlHud.Instance invalid?");
            MelonLogger.Msg("CohtmlHud.Instance: " + CohtmlHud.Instance);
            throw;
        }
    }

    //hopefully whatever rework was hinted at doesn't immediatly break this
    private static void SetMovementSystem(bool isVR)
    {
        try
        {
            MelonLogger.Msg($"Set MovementSystem instance to {isVR}.");
            MovementSystem.Instance.isVr = true;
        }
        catch (Exception)
        {
            MelonLogger.Error("Setting MovementSystem isVr failed. Is MovementSystem.Instance invalid?");
            MelonLogger.Msg("MovementSystem.Instance: " + MovementSystem.Instance);
            throw;
        }
    }

    private static void ReloadCVRInputManager()
    {
        try
        {
            MelonLogger.Msg("Set CVRInputManager reload to True. Input should reload next frame...");
            CVRInputManager.Instance.reload = true;
            //just in case
            CVRInputManager.Instance.inputEnabled = true;
            CVRInputManager.Instance.blockedByUi = false;
            //sometimes head can get stuck, so just in case
            CVRInputManager.Instance.independentHeadToggle = false;
            //just nice to load into desktop with idle gesture
            CVRInputManager.Instance.gestureLeft = 0f;
            CVRInputManager.Instance.gestureLeftRaw = 0f;
            CVRInputManager.Instance.gestureRight = 0f;
            CVRInputManager.Instance.gestureRightRaw = 0f;
        }
        catch (Exception)
        {
            MelonLogger.Error("CVRInputManager reload failed. Is CVRInputManager.Instance invalid?");
            MelonLogger.Msg("CVRInputManager.Instance: " + CVRInputManager.Instance);
            throw;
        }
    }

    private static void Recalibrate()
    {
        try
        {
            MelonLogger.Msg("Called ReCalibrateAvatar() on PlayerSetup.Instance. Will take a second...");
            PlayerSetup.Instance.ReCalibrateAvatar();
        }
        catch (Exception)
        {
            MelonLogger.Error("ReCalibrateAvatar() failed. Is PlayerSetup.Instance invalid?");
            MelonLogger.Msg("PlayerSetup.Instance: " + PlayerSetup.Instance);
            throw;
        }
    }
}