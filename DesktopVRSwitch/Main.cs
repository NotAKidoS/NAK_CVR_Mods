using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.EventSystem;
using ABI_RC.Systems.IK.SubSystems;
using MelonLoader;
using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using HarmonyLib;
using Object = UnityEngine.Object;


//tell the game to change VRMode/DesktopMode for Steam/Discord presence
//RichPresence.PopulatePresence();

//nvm that resets the RichPresence clock- i want people to know how long ive wasted staring at mirror 



namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    private static MelonPreferences_Category m_categoryDesktopVRSwitch;
    private static MelonPreferences_Entry<bool> m_entryReloadInstance;
    private static MelonPreferences_Entry<bool> m_entryTimedErrorCatch;
    public override void OnApplicationStart()
    {
        m_categoryDesktopVRSwitch = MelonPreferences.CreateCategory(nameof(DesktopVRSwitch));
        //m_entryReloadInstance = m_categoryDesktopVRSwitch.CreateEntry<bool>("Rejoin Instance", false, description: "Rejoin instance on switch.");
        m_entryTimedErrorCatch = m_categoryDesktopVRSwitch.CreateEntry<bool>("Timed Error Catch", true, description: "Attempt to switch back if an error is found after 10 seconds.");

        m_categoryDesktopVRSwitch.SaveToFile(false);
    }

    private static bool isAttemptingSwitch = false;
    private static float timedSwitch = 0f;
    private static bool CurrentMode;
    private static Vector3 avatarPos;
    private static Quaternion avatarRot;

    public override void OnUpdate()
    {
        // assuming CVRInputManager.switchMode button was originally for desktop/vr switching before being left to do literally nothing in rootlogic
        if (Input.GetKeyDown(KeyCode.F6) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && !isAttemptingSwitch)
        {
            //start attempt
            isAttemptingSwitch = true;
            MelonCoroutines.Start(AttemptPlatformSwitch());

            //how long we wait until we assume an error occured
            if (m_entryTimedErrorCatch.Value)
                timedSwitch = Time.time + 10f;
        }

        //catch if coroutine just decided to not finish... which happens?
        if (isAttemptingSwitch && Time.time > timedSwitch)
        {
            MelonLogger.Error("Timer exceeded. Something is wrong and coroutine failed partway.");
            MelonCoroutines.Start(AttemptPlatformSwitch(true));
        }

        //correct player position while switching
        if (isAttemptingSwitch && !MovementSystem.Instance.canMove)
        {
            MovementSystem.Instance.TeleportToPosRot(avatarPos, avatarRot, false);
            MovementSystem.Instance.UpdateColliderCenter(avatarPos);
        }
    }

    private static IEnumerator AttemptPlatformSwitch(bool forceMode = false)
    {
        //forceMode will attempt to backtrack to last working mode (if you dont like the mess, fix it yourself thx)
        CurrentMode = forceMode ? CurrentMode : MetaPort.Instance.isUsingVr;
        bool VRMode = forceMode ? CurrentMode : !CurrentMode;

        //store current player position/rotation to correct VR/Desktop offsets
        avatarPos = PlayerSetup.Instance._avatar.transform.position;
        avatarRot = PlayerSetup.Instance._avatar.transform.rotation;

        //prevent player from any movement while transitioning
        MovementSystem.Instance.canMove = false;
        BodySystem.TrackingEnabled = false;

        //load SteamVR
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

        //needs to come after SetMovementSystem
        //UpdateGestureReconizerCam();

        yield
        return new WaitForEndOfFrame();

        //right here is the fucker most likely to break
        ReloadCVRInputManager();

        //some menus have 0.5s wait(), so to be safe
        yield
        return new WaitForSeconds(0.5f);

        //reload current avatar
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);

        yield
        return new WaitForSeconds(2f);

        if (!VRMode)
            //collision center is set to match headpos in VR, but desktop doesnt reset it
            MovementSystem.Instance.UpdateColliderCenter(PlayerSetup.Instance._avatar.transform.position);

        //gonna try doing this last
        DisposeSteamVR(VRMode);

        MovementSystem.Instance.canMove = true;
        BodySystem.TrackingEnabled = true;

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
    }

    private static void DisposeSteamVR(bool isVR)
    {
        if (!isVR)
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

        //disable input during switch
        CVRInputManager.Instance.inputEnabled = false;
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
            CohtmlHud.Instance.gameObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        }
        catch (Exception)
        {
            MelonLogger.Error("Error parenting CohtmlHud to active camera. Is CohtmlHud.Instance invalid?");
            MelonLogger.Msg("CohtmlHud.Instance: " + CohtmlHud.Instance);
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

    //every nameplate canvas uses CameraFacingObject :stare:
    private static void UpdateCameraFacingObject()
    {
        try
        {
            MelonLogger.Msg("Updating all CameraFacingObject scripts to face new camera. (this fixes nameplates)");
            CameraFacingObject[] camfaceobjs = Object.FindObjectsOfType<CameraFacingObject>();

            for (int i = 0; i < camfaceobjs.Count(); i++)
            {
                camfaceobjs[i].m_Camera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            }
        }
        catch (Exception)
        {
            MelonLogger.Error("Error updating CameraFacingObject objects! Nameplates will be wonk...");
            throw;
        }
    }

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

    //this doesnt seem to work
    private static void UpdateGestureReconizerCam()
    {
        try
        {
            MelonLogger.Msg("Set GestureReconizerCam camera to Camera.main.");
            Camera cam = Traverse.Create(CVRGestureRecognizer.Instance).Field("_camera").GetValue() as Camera;
            cam = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        }
        catch (Exception)
        {
            MelonLogger.Error("Error updating CVRGestureRecognizer camera!");
            throw;
        }
    }
}