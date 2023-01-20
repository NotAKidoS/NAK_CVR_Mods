using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.Camera;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.IK.TrackingModules;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.Networking;
using DesktopVRSwitch.Patches;
using HarmonyLib;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Object = UnityEngine.Object;

namespace DesktopVRSwitch;

public class DesktopVRSwitchHelper : MonoBehaviour
{
    public static DesktopVRSwitchHelper Instance;

    //settings
    public bool SettingTimedErrorCatch = true;
    public float SettingTimedErrorTimer = 10f;

    //internal shit
    internal static bool isAttemptingSwitch = false;
    internal static float timedSwitch = 0f;
    internal static bool CurrentMode;
    internal static Vector3 avatarWorldPos;
    internal static Quaternion avatarWorldRot;

    public void SwitchMode(bool isTimedSwitch = false)
    {
        if (isAttemptingSwitch) return;

        isAttemptingSwitch = true;
        MelonCoroutines.Start(AttemptPlatformSwitch());

        //how long we wait until we assume an error occured
        if (isTimedSwitch)
            timedSwitch = Time.time + SettingTimedErrorTimer;
    }

    public void Start()
    {
        Instance = this;
    }

    public void Update()
    {
        // assuming CVRInputManager.switchMode button was originally for desktop/vr switching before being left to do literally nothing in rootlogic
        if (Input.GetKeyDown(KeyCode.F6) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && !isAttemptingSwitch)
        {
            SwitchMode(true);
        }

        if (!isAttemptingSwitch) return;

        //catch if coroutine just decided to not finish... which happens?
        if (Time.time > timedSwitch)
        {
            MelonLogger.Error("Timer exceeded. Something is wrong and coroutine failed partway.");
            isAttemptingSwitch = false;
            if (SettingTimedErrorCatch)
                SwitchMode();
        }
    }

    //disables VRIK if it was on the current avatar during switch
    //absolutely bruteforcing the stupid vr playspace offset issue
    public void LateUpdate()
    {
        if (!isAttemptingSwitch) return;

        if (!PlayerSetup.Instance.avatarIsLoading && PlayerSetup.Instance._avatar != null)
        {
            BodySystem.TrackingEnabled = false;
            BodySystem.TrackingPositionWeight = 0f;
            PlayerSetup.Instance._avatar.transform.position = avatarWorldPos;
            PlayerSetup.Instance._avatar.transform.rotation = avatarWorldRot;
            MovementSystem.Instance.TeleportToPosRot(avatarWorldPos, avatarWorldRot, false);
            MovementSystem.Instance.UpdateColliderCenter(avatarWorldPos);
        }
    }

    internal static IEnumerator AttemptPlatformSwitch(bool forceMode = false)
    {
        //forceMode will attempt to backtrack to last working mode (if you dont like the mess, fix it yourself thx)
        CurrentMode = forceMode ? CurrentMode : MetaPort.Instance.isUsingVr;
        bool VRMode = forceMode ? CurrentMode : !CurrentMode;

        CloseMenuElements(VRMode);
        ToggleInputInteractions(false);
        DisableMirrorCanvas();

        //store current player position/rotation to correct VR/Desktop offsets
        avatarWorldPos = PlayerSetup.Instance._avatar.transform.position;
        avatarWorldRot = PlayerSetup.Instance._avatar.transform.rotation;

        //exit all movement states
        MovementSystem.Instance.ChangeCrouch(false);
        MovementSystem.Instance.ChangeProne(false);
        MovementSystem.Instance.canMove = false;
        MovementSystem.Instance.canRot = false;

        //load SteamVR
        InitializeSteamVR(VRMode);

        yield
        return new WaitForEndOfFrame();

        SetCheckVR(VRMode);
        SetMetaPort(VRMode);

        //reset rich presence
        if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceDiscordEnabled", true))
        {
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", false);
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", true);
        }
        if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceSteamEnabled", true))
        {
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", false);
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", true);
        }

        yield
        return new WaitForEndOfFrame();

        SwitchActiveCameraRigs(VRMode);
        UpdateCameraFacingObject();
        RepositionCohtmlHud(VRMode);
        UpdateHudOperations(VRMode);
        SwitchPickupOrigins();

        yield
        return new WaitForEndOfFrame();

        //needs to come after SetMovementSystem
        UpdateGestureReconizerCam();

        ResetCVRInputManager();

        //gonna try doing this last
        DisposeSteamVR(VRMode);

        ToggleInputInteractions(true);

        //reload current avatar
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);

        yield return new WaitUntil(() => !PlayerSetup.Instance.avatarIsLoading);

        isAttemptingSwitch = false;

        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        MovementSystem.Instance.canMove = true;
        MovementSystem.Instance.canRot = true;

        if (!VRMode)
            //collision center is set to match headpos in VR, but desktop doesnt reset it
            MovementSystem.Instance.UpdateColliderCenter(PlayerSetup.Instance._avatar.transform.position);

        yield
        return new WaitForEndOfFrame();

        //one last teleport to correct VR offset
        MovementSystem.Instance.TeleportToPosRot(avatarWorldPos, avatarWorldRot, false);

        yield
        return null;
    }

    //shitton of try catch below

    internal static void InitializeSteamVR(bool isVR)
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

    internal static void DisposeSteamVR(bool isVR)
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
    internal static void CloseMenuElements(bool isVR)
    {
        if (ViewManager.Instance != null)
        {
            MelonLogger.Msg("Closed MainMenu Instance.");
            ViewManager.Instance.UiStateToggle(false);
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

    internal static void ToggleInputInteractions(bool toggle)
    {
        //disable input during switch
        try
        {
            MelonLogger.Msg($"Toggling input & interactions to " + toggle);
            CVRInputManager.Instance.inputEnabled = toggle;
            CVR_InteractableManager.enableInteractions = toggle;
        }
        catch
        {
            MelonLogger.Error("Toggling input & interactions failed. Is something invalid?");
            MelonLogger.Msg("CVRInputManager.Instance: " + CVRInputManager.Instance);
            MelonLogger.Msg("CVR_InteractableManager: " + CVR_InteractableManager.enableInteractions);
            throw;
        }
    }

    internal static void SetCheckVR(bool isVR)
    {
        try
        {
            MelonLogger.Msg($"Set CheckVR hasVrDeviceLoaded to {isVR}.");
            CheckVR.Instance.hasVrDeviceLoaded = isVR;
        }
        catch
        {
            MelonLogger.Error("Setting CheckVR hasVrDeviceLoaded failed. Is CheckVR.Instance invalid?");
            MelonLogger.Msg("CheckVR.Instance: " + CheckVR.Instance);
            throw;
        }
    }

    internal static void SetMetaPort(bool isVR)
    {
        try
        {
            MelonLogger.Msg($"Set MetaPort isUsingVr to {isVR}.");
            MetaPort.Instance.isUsingVr = isVR;
        }
        catch
        {
            MelonLogger.Error("Setting MetaPort isUsingVr failed. Is MetaPort.Instance invalid?");
            MelonLogger.Msg("MetaPort.Instance: " + MetaPort.Instance);
            throw;
        }
    }

    internal static void SwitchActiveCameraRigs(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Switched active camera rigs.");
            PlayerSetup.Instance.desktopCameraRig.SetActive(!isVR);
            PlayerSetup.Instance.vrCameraRig.SetActive(isVR);
        }
        catch
        {
            MelonLogger.Error("Error switching active cameras. Are the camera rigs invalid?");
            MelonLogger.Msg("PlayerSetup.Instance.desktopCameraRig: " + PlayerSetup.Instance.desktopCameraRig);
            MelonLogger.Msg("PlayerSetup.Instance.vrCameraRig: " + PlayerSetup.Instance.vrCameraRig);
            throw;
        }
    }

    internal static void RepositionCohtmlHud(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Parented CohtmlHud to active camera.");
            CohtmlHud.Instance.gameObject.transform.parent = isVR ? PlayerSetup.Instance.vrCamera.transform : PlayerSetup.Instance.desktopCamera.transform;

            //sets hud position, rotation, ~~and scale~~ based on MetaPort isUsingVr
            CVRTools.ConfigureHudAffinity();
            CohtmlHud.Instance.gameObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        }
        catch
        {
            MelonLogger.Error("Error parenting CohtmlHud to active camera. Is CohtmlHud.Instance invalid?");
            MelonLogger.Msg("CohtmlHud.Instance: " + CohtmlHud.Instance);
            throw;
        }
    }

    internal static void ResetCVRInputManager()
    {
        try
        {
            MelonLogger.Msg("Enabling CVRInputManager inputEnabled & disabling blockedByUi!");
            //CVRInputManager.Instance.reload = true;
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
        catch
        {
            MelonLogger.Error("CVRInputManager reload failed. Is CVRInputManager.Instance invalid?");
            MelonLogger.Msg("CVRInputManager.Instance: " + CVRInputManager.Instance);
            throw;
        }
    }

    //every nameplate canvas uses CameraFacingObject :stare:
    internal static void UpdateCameraFacingObject()
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
        catch
        {
            MelonLogger.Error("Error updating CameraFacingObject objects! Nameplates will be wonk...");
            throw;
        }
    }

    internal static void UpdateHudOperations(bool isVR)
    {
        try
        {
            MelonLogger.Msg("Set HudOperations worldLoadingItem and worldLoadStatus to their respective Desktop/Vr parent.");
            HudOperations.Instance.worldLoadingItem = isVR ? HudOperations.Instance.worldLoadingItemVr : HudOperations.Instance.worldLoadingItemDesktop;
            HudOperations.Instance.worldLoadStatus = isVR ? HudOperations.Instance.worldLoadStatusVr : HudOperations.Instance.worldLoadStatusDesktop;
        }
        catch
        {
            MelonLogger.Error("Error updating HudOperations LoadingItem & LoadStatus!");
            throw;
        }
    }

    internal static void DisableMirrorCanvas()
    {
        try
        {
            //tell the game we are in mirror mode so itll disable it (if enabled)
            PortableCamera.Instance.mode = MirroringMode.Mirror;
            PortableCamera.Instance.ChangeMirroring();
        }
        catch
        {
            MelonLogger.Error("Error updating CVRGestureRecognizer camera!");
            throw;
        }
    }

    internal static void UpdateGestureReconizerCam()
    {
        try
        {
            MelonLogger.Msg("Set GestureReconizerCam camera to active camera.");
            Traverse.Create(CVRGestureRecognizer.Instance).Field("_camera").SetValue(PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>());
        }
        catch
        {
            MelonLogger.Error("Error updating CVRGestureRecognizer camera!");
            throw;
        }
    }

    internal static void SwitchPickupOrigins()
    {
        try
        {
            MelonLogger.Msg("Switched pickup origins.");
            CVRPickupObjectTracker[] pickups = Object.FindObjectsOfType<CVRPickupObjectTracker>();
            for (int i = 0; i < pickups.Count(); i++)
            {
                pickups[i].OnSwitch();
            }
        }
        catch
        {
            MelonLogger.Error("Error switching pickup origins!");
            throw;
        }
    }
}