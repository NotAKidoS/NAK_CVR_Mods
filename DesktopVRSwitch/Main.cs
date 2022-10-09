using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.EventSystem;
using MelonLoader;
using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using HarmonyLib;
using Object = UnityEngine.Object;

//Remove VRIK on VR to Desktop
//Remove LookAtIK on Desktop to VR

//Set Desktop camera to head again...?
//Recenter collision position (in VR it shifts around)



namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    private static bool isAttemptingSwitch = false;
    private static float timedSwitch = 0f;
    private static bool CurrentMode;

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
            MelonLogger.Error("Timer exceeded. Something is wrong and coroutine failed partway.");
            MelonCoroutines.Start(AttemptPlatformSwitch(true));
        }
    }

    private static IEnumerator AttemptPlatformSwitch(bool forceMode = false)
    {
        //forceMode will attempt to backtrack to last working mode (if you dont like the mess, fix it yourself thx)
        CurrentMode = forceMode ? CurrentMode : MetaPort.Instance.isUsingVr;
        bool VRMode = forceMode ? CurrentMode : !CurrentMode;

        //load or unload SteamVR
        InitializeSteamVR(VRMode);

        CloseMenuElements(VRMode);

        yield
        return new WaitForEndOfFrame();

        SetMetaPort(VRMode);

        yield
        return new WaitForEndOfFrame();

        SetPlayerSetup(VRMode);
        SwitchActiveCameraRigs(VRMode);
        UpdateCameraFacingObject();
        RepositionCohtmlHud(VRMode);
        UpdateHudOperations(VRMode);

        yield
        return new WaitForEndOfFrame();

        RemoveComponents(VRMode);

        yield
        return new WaitForEndOfFrame();

        SetMovementSystem(VRMode);

        yield
        return new WaitForEndOfFrame();

        //needs to come after SetMovementSystem
        UpdateGestureReconizerCam();

        yield
        return new WaitForSeconds(0.5f);

        //right here is the fucker most likely to break
        ReloadCVRInputManager();

        //some menus have 0.5s wait(), so to be safe
        yield
        return new WaitForSeconds(0.5f);

        //I am setting the collision center to the avatars position so the collision is set in the same place as where it was after the player moved roomscale in VR

        //need to recenter player avatar as VRIK locomotion moves that directly
        Vector3 roomscalePos = PlayerSetup.Instance._avatar.transform.position;
        Quaternion roomscaleRot = PlayerSetup.Instance._avatar.transform.rotation;

        MovementSystem.Instance.enabled = false;
        MovementSystem.Instance.transform.position = roomscalePos;
        MovementSystem.Instance.transform.rotation = roomscaleRot;
        MovementSystem.Instance.enabled = true;

        //collision center is set to match headpos in VR, but desktop doesnt reset it
        //MovementSystem.Instance.proxyCollider.center = Vector3.zero; //not sure why UpdateColliderCenter doesnt do this
        MovementSystem.Instance.UpdateColliderCenter(roomscalePos);

        //AssetManagement.Instance.LoadLocalAvatar(this.avatarId);

        //tell the game to change VRMode/DesktopMode for Steam/Discord presence
        //RichPresence.PopulatePresence();

        //nvm that resets the RichPresence clock- i want people to know how long ive wasted staring at mirror 

        yield
        return null;
        isAttemptingSwitch = false;
    }

    //shitton of try catch below

    private static void InitializeSteamVR(bool isVR)
    {
        if (isVR)
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

    private static void RemoveComponents(bool isVR)
    {
        try
        {
            if (!isVR)
            {
                MelonLogger.Msg("VRIK component is not needed. Removing.");
                VRIK ik = (VRIK)PlayerSetup.Instance._avatar.GetComponent(typeof(VRIK));
                if (ik != null)
                {
                    UnityEngine.Object.Destroy(ik);
                }
            }
            else
            {
                MelonLogger.Msg("LookIK component is not needed. Removing.");
                LookAtIK ik = (LookAtIK)PlayerSetup.Instance._avatar.GetComponent(typeof(LookAtIK));
                if (ik != null)
                {
                    UnityEngine.Object.Destroy(ik);
                }
            }

            MelonLogger.Msg("Removing Viseme and Eye controllers.");
            CVRVisemeController cvrvisemeController = (CVRVisemeController)PlayerSetup.Instance._avatar.GetComponent(typeof(CVRVisemeController));
            if (cvrvisemeController != null)
            {
                UnityEngine.Object.Destroy(cvrvisemeController);
            }
            CVREyeController cvreyeController = (CVREyeController)PlayerSetup.Instance._avatar.GetComponent(typeof(CVREyeController));
            if (cvreyeController != null)
            {
                UnityEngine.Object.Destroy(cvreyeController);
            }
        }
        catch (Exception)
        {
            MelonLogger.Error("Temp creation of VRIK on avatar failed. Is PlayerSetup.Instance invalid?");
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

            //sets hud position, rotation, and scale based on MetaPort isUsingVr
            CVRTools.ConfigureHudAffinity();
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
            PlayerSetup.Instance.CalibrateAvatar();
        }
        catch (Exception)
        {
            MelonLogger.Error("ReCalibrateAvatar() failed. Is PlayerSetup.Instance invalid?");
            MelonLogger.Msg("PlayerSetup.Instance: " + PlayerSetup.Instance);
            throw;
        }
    }

    //every nameplate canvas uses CameraFacingObject :stare:
    //might need to use actual Desktop/VR cam instead of Camera.main
    private static void UpdateCameraFacingObject()
    {
        try
        {
            MelonLogger.Msg("Updating all CameraFacingObject scripts to face new camera. (this fixes nameplates)");
            CameraFacingObject[] camfaceobjs = Object.FindObjectsOfType<CameraFacingObject>();

            for (int i = 0; i < camfaceobjs.Count(); i++)
            {
                camfaceobjs[i].m_Camera = Camera.main;
            }
        }
        catch (Exception)
        {
            MelonLogger.Error("Error updating CameraFacingObject objects! Nameplates will be wonk...");
            throw;
        }
    }

    //cant fix unless i log the original VR gripOrigins with a patch...
    //private static void SetPickupObjectOrigins()
    //{
    //    try
    //    {
    //        CVRPickupObject[] pickups = Object.FindObjectsOfType<CVRPickupObject>();

    //        if (pickups.gripOrigin != null)
    //        {
    //            Transform x = this.gripOrigin.Find("[Desktop]");
    //            if (x != null)
    //            {
    //                this.gripOrigin = x;
    //            }
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        MelonLogger.Error("Error updating CameraFacingObject objects! Nameplates will be wonk...");
    //        throw;
    //    }
    //}

    private static void UpdateHudOperations(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Set HudOperations worldLoadingItem and worldLoadStatus to their respective Desktop/Vr parent.");
            HudOperations.Instance.worldLoadingItem = isVR ? HudOperations.Instance.worldLoadingItemVr : HudOperations.Instance.worldLoadingItemDesktop;
            HudOperations.Instance.worldLoadStatus = isVR ? HudOperations.Instance.worldLoadStatusVr : HudOperations.Instance.worldLoadStatusDesktop;
        }
        catch (Exception)
        {
            MelonLogger.Error("Error updating HudOperations LoadingItem & LoadStatus!");
            throw;
        }
    }

    //i suck at traverse
    private static void UpdateGestureReconizerCam()
    {
        try
        {
            MelonLogger.Msg("Set GestureReconizerCam camera to Camera.main.");
            Camera cam = Traverse.Create(CVRGestureRecognizer.Instance).Field("_camera").GetValue() as Camera;
            cam = Camera.main;
        }
        catch (Exception)
        {
            MelonLogger.Error("Error updating CVRGestureRecognizer camera!");
            throw;
        }
    }
}