using HarmonyLib;
using MelonLoader;
using UnityEngine;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.UI;
using System.Reflection;
using cohtml;
using ABI_RC.Core.IO;
using System.Collections;
using System;
using UnityEngine.XR;
using Valve.VR;

namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{

    private static MelonPreferences_Category m_categoryDesktopVRSwitch;
    private static MelonPreferences_Entry<bool> m_entryEnabled;

    private static System.Object melon;

    public override void OnApplicationStart()
    {
        m_categoryDesktopVRSwitch = MelonPreferences.CreateCategory(nameof(DesktopVRSwitch));
        m_entryEnabled = m_categoryDesktopVRSwitch.CreateEntry<bool>("Start SteamVR", false, description: "Launch SteamVR");
        m_entryEnabled.OnValueChangedUntyped += UpdateSettings;
    }
    private static void UpdateSettings()
    {
        melon = MelonCoroutines.Start(AttemptPlatformSwitch());
    }

    static IEnumerator AttemptPlatformSwitch()
    {
        if (MetaPort.Instance.isUsingVr)
        {
            //close the UI cause cohtml can get grumpy
            ViewManager.Instance.UiStateToggle(false);
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
            MelonLogger.Msg("Closed ChilloutVR UI.");

            MetaPort.Instance.isUsingVr = false;
            MelonLogger.Msg("Set MetaPort isUsingVr to false.");

            SteamVR_Behaviour.instance.enabled = false;
            SteamVR_Render.instance.enabled = false;

            yield return new WaitForEndOfFrame();

            PlayerSetup.Instance._inVr = false;
            PlayerSetup.Instance.Invoke("CalibrateAvatar", 0f);
            MelonLogger.Msg("Invoked CalibrateAvatar() on PlayerSetup.Instance.");
            PlayerSetup.Instance.desktopCameraRig.SetActive(true);
            PlayerSetup.Instance.vrCameraRig.SetActive(false);
            CohtmlHud.Instance.gameObject.transform.parent = PlayerSetup.Instance.desktopCamera.transform;
            MelonLogger.Msg("Enabled Desktop camera rig.");
            MelonLogger.Msg("Set PlayerSetup _inVr to false.");

            yield return new WaitForEndOfFrame();

            MovementSystem.Instance.isVr = false;
            MelonLogger.Msg("Set MovementSystem isVr to false.");
            //CVR_MovementSystem.Instance.isVr = true;
            //MelonLogger.Msg("Set CVR_MovementSystem isVR to false.");

            yield return new WaitForSeconds(0.5f);

            CVRInputManager.Instance.reload = true;
            //CVRInputManager.Instance.inputEnabled = true;
            //CVRInputManager.Instance.blockedByUi = false;
            //CVRInputManager.Instance.independentHeadToggle = false;
            //MelonLogger.Msg("Set CVRInputManager reload to true. Input should reload next frame...");

            yield return new WaitForSeconds(1f);

            CVRInputManager.Instance.reload = true;

            yield return new WaitForSeconds(0.5f);

            XRSettings.enabled = false;

            PlayerSetup.Instance.Invoke("CalibrateAvatar", 0f);
            MelonLogger.Msg("Invoked CalibrateAvatar() on PlayerSetup.Instance.");
            //ViewManager.Instance.VrInputChanged(true);
        }
        else
        {
            //close the UI cause cohtml can get grumpy
            ViewManager.Instance.UiStateToggle(false);
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
            MelonLogger.Msg("Closed ChilloutVR UI.");

            MelonCoroutines.Start(LoadDevice("OpenVR"));
            MelonLogger.Msg("OpenVR device loaded!");

            yield return new WaitForSeconds(3);

            MelonLogger.Msg("Set MetaPort isUsingVr to true.");
            MetaPort.Instance.isUsingVr = true;

            yield return new WaitForEndOfFrame();

            VRIK ik = (VRIK)PlayerSetup.Instance._avatar.GetComponent(typeof(VRIK));
            if (ik == null)
            {
                ik = PlayerSetup.Instance._avatar.AddComponent<VRIK>();
            }
            ik.solver.IKPositionWeight = 0f;
            ik.enabled = false;

            PlayerSetup.Instance._inVr = true;
            PlayerSetup.Instance.Invoke("CalibrateAvatar", 0f);
            MelonLogger.Msg("Invoked CalibrateAvatar() on PlayerSetup.Instance.");
            PlayerSetup.Instance.desktopCameraRig.SetActive(false);
            PlayerSetup.Instance.vrCameraRig.SetActive(true);
            CohtmlHud.Instance.gameObject.transform.parent = PlayerSetup.Instance.vrCamera.transform;
            MelonLogger.Msg("Disabled Desktop camera rig.");
            MelonLogger.Msg("Set PlayerSetup _inVr to true.");

            yield return new WaitForEndOfFrame();

            MovementSystem.Instance.isVr = true;
            MelonLogger.Msg("Set MovementSystem isVr to false.");
            //CVR_MovementSystem.Instance.isVr = true;
            //MelonLogger.Msg("Set CVR_MovementSystem isVR to false.");

            yield return new WaitForSeconds(0.5f);

            CVRInputManager.Instance.reload = true;
            //CVRInputManager.Instance.inputEnabled = true;
            //CVRInputManager.Instance.blockedByUi = false;
            //CVRInputManager.Instance.independentHeadToggle = false;
            //MelonLogger.Msg("Set CVRInputManager reload to true. Input should reload next frame...");

            yield return new WaitForSeconds(1f);

            CVRInputManager.Instance.reload = true;

            //PlayerSetup.Instance.Invoke("CalibrateAvatar", 0f);
            //MelonLogger.Msg("Invoked CalibrateAvatar() on PlayerSetup.Instance.");

            //ViewManager.Instance.VrInputChanged(true);
        }

        yield return null;
    }

    static IEnumerator LoadDevice(string newDevice)
    {
        if (String.Compare(XRSettings.loadedDeviceName, newDevice, true) != 0)
        {
            XRSettings.LoadDeviceByName(newDevice);
            yield return null;
            XRSettings.enabled = true;
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
        }
    }
}