using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.Camera;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.InputManagement.InputModules;
using HarmonyLib;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch;

internal class TryCatchHell
{
    internal static void TryCatchWrapper(Action action, string errorMsg, params object[] msgArgs)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            DesktopXRSwitch.Logger.Error(string.Format(errorMsg, msgArgs));
            DesktopXRSwitch.Logger.Msg(ex.Message);
        }
    }

    internal static void CloseCohtmlMenus()
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Closing ViewManager & CVR_MenuManager menus.");
            ViewManager.Instance.UiStateToggle(false);
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
        },
        "Setting CheckVR hasVrDeviceLoaded failed.");
    }

    internal static void SetCheckVR(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg($"Setting CheckVR hasVrDeviceLoaded to {isXR}.");
            CheckVR.Instance.hasVrDeviceLoaded = isXR;
        },
        "Setting CheckVR hasVrDeviceLoaded failed.");
    }

    internal static void SetMetaPort(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg($"Setting MetaPort isUsingVr to {isXR}.");
            MetaPort.Instance.isUsingVr = isXR;
        },
        "Setting MetaPort isUsingVr failed.");
    }

    internal static void RepositionCohtmlHud(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Configuring new hud affinity for CohtmlHud.");
            CohtmlHud.Instance.gameObject.transform.parent = isXR ? PlayerSetup.Instance.vrCamera.transform : PlayerSetup.Instance.desktopCamera.transform;
            CVRTools.ConfigureHudAffinity();
            CohtmlHud.Instance.gameObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        },
        "Error parenting CohtmlHud to active camera.");
    }

    internal static void UpdateHudOperations(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Switching HudOperations worldLoadingItem & worldLoadStatus.");
            HudOperations.Instance.worldLoadingItem = isXR ? HudOperations.Instance.worldLoadingItemVr : HudOperations.Instance.worldLoadingItemDesktop;
            HudOperations.Instance.worldLoadStatus = isXR ? HudOperations.Instance.worldLoadStatusVr : HudOperations.Instance.worldLoadStatusDesktop;
        },
        "Failed switching HudOperations objects.");
    }

    internal static void DisableMirrorCanvas()
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Forcing PortableCamera canvas mirroring off.");
            //tell the game we are in mirror mode so itll disable it (if enabled)
            PortableCamera.Instance.mode = MirroringMode.Mirror;
            PortableCamera.Instance.ChangeMirroring();
        },
        "Failed to disable PortableCamera canvas mirroring.");
    }

    internal static void SwitchActiveCameraRigs(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Switching active PlayerSetup camera rigs. Updating Desktop camera FOV.");
            PlayerSetup.Instance.desktopCameraRig.SetActive(!isXR);
            PlayerSetup.Instance.vrCameraRig.SetActive(isXR);
            CVR_DesktopCameraController.UpdateFov();
            //uicamera has script that copies fov from desktop cam
            //toggling the cameras on/off resets aspect ratio
            //so when rigs switch, that is already handled
        },
        "Failed to switch active camera rigs or update Desktop camera FOV.");
    }

    internal static void PauseInputInteractions(bool toggle)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg($"Setting CVRInputManager inputEnabled & CVR_InteractableManager enableInteractions to {!toggle}");
            CVRInputManager.Instance.inputEnabled = !toggle;
            CVR_InteractableManager.enableInteractions = !toggle;
        },
        "Failed to toggle CVRInputManager inputEnabled & CVR_InteractableManager enableInteractions.");
    }

    internal static void SwitchCVRInputManager(bool isXR)
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Resetting CVRInputManager inputs.");
            //just in case
            CVRInputManager.Instance.blockedByUi = false;
            //sometimes head can get stuck, so just in case
            CVRInputManager.Instance.independentHeadToggle = false;
            //just nice to load into desktop with idle gesture
            CVRInputManager.Instance.gestureLeft = 0f;
            CVRInputManager.Instance.gestureLeftRaw = 0f;
            CVRInputManager.Instance.gestureRight = 0f;
            CVRInputManager.Instance.gestureRightRaw = 0f;
            //turn off finger tracking input
            CVRInputManager.Instance.individualFingerTracking = false;

            //add input module if you started in desktop
            if (CVRInputManager._moduleOpenXR == null)
            {
                CVRInputManager.Instance.AddInputModule(CVRInputManager._moduleOpenXR = new CVRInputModule_OpenXR());
            }

            //enable xr input or whatnot
            CVRInputManager._moduleOpenXR.InputEnabled = isXR;
        },
        "Failed to reset CVRInputManager inputs.");
    }

    internal static void ReloadLocalAvatar()
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Attempting to reload current local avatar from GUID.");
            AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);
        },
        "Failed to reload local avatar.");
    }

    internal static void UpdateRichPresence()
    {
        TryCatchWrapper(() =>
        {
            if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceDiscordEnabled", true))
            {
                DesktopXRSwitch.Logger.Msg("Forcing Discord Rich Presence update.");
                MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", true);
            }
            if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceSteamEnabled", true))
            {
                DesktopXRSwitch.Logger.Msg("Forcing Steam Rich Presence update.");
                MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", true);
            }
        },
        "Failed to update Discord & Steam Rich Presence.");
    }

    internal static void UpdateGestureReconizerCam()
    {
        TryCatchWrapper(() =>
        {
            DesktopXRSwitch.Logger.Msg("Updating CVRGestureRecognizer _camera to active camera.");
            Traverse.Create(CVRGestureRecognizer.Instance).Field("_camera").SetValue(PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>());
        },
        "Failed to update CVRGestureRecognizer camera.");
    }
}