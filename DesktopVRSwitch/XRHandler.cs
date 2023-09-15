using System.Collections;
using System.Reflection;
using ABI_RC.Core.Savior;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Valve.VR;

namespace NAK.DesktopVRSwitch;

internal static class XRHandler
{
    internal static IEnumerator StartXR()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        else
            yield return StopXR();

        yield return null;
    }

    internal static IEnumerator StopXR()
    {
        if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            yield break;
        
        // Forces SteamVR to reinitialize SteamVR_Input next switch
        SteamVR_ActionSet_Manager.DisableAllActionSets();
        SteamVR_Input.initialized = false;

        // Remove SteamVR behaviour & render
        UnityEngine.Object.DestroyImmediate(SteamVR_Behaviour.instance.gameObject);
        SteamVR.enabled = false; // disposes SteamVR

        // Disable UnityXR
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();

        // We don't really need to wait a frame on Stop()
        yield return null;
    }
    
    internal static void SwitchLoader()
    {
        XRLoader item;
        
        if (!CheckVR.Instance.forceOpenXr)
        {
            item = ScriptableObject.CreateInstance<OpenVRLoader>();
            DesktopVRSwitch.Logger.Msg("Using XR Loader: SteamVR");
        }
        else
        {
            item = ScriptableObject.CreateInstance<OpenXRLoader>();
            DesktopVRSwitch.Logger.Msg("Using XR Loader: OpenXR");
        }

        typeof(XRManagerSettings)
            .GetField("m_Loaders", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(XRGeneralSettings.Instance.Manager, new List<XRLoader> { item });
    }
}
