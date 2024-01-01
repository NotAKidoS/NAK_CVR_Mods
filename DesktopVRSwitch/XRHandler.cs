#if !PLATFORM_ANDROID
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.Savior;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Valve.VR;
using Object = UnityEngine.Object;

namespace ABI_RC.Systems.VRModeSwitch
{
    internal static class XRHandler
    {
        private static async Task InitializeXRLoader()
        {
            EnsureXRLoader();
            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            await Task.Yield();
        }
        
        internal static async Task StartXR()
        {
            await InitializeXRLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            else
                await StopXR(); // assuming StopXR is now an async method.

            // Await a delay or equivalent method to wait for a frame.
            await Task.Yield(); // This line is to simulate "waiting for the next frame" in an async way.
        }

        internal static Task StopXR()
        {
            if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
                return Task.CompletedTask;

            // Forces SteamVR to reinitialize SteamVR_Input next switch
            SteamVR_ActionSet_Manager.DisableAllActionSets();
            SteamVR_Input.initialized = false;

            // Remove SteamVR behaviour & render
            Object.DestroyImmediate(SteamVR_Behaviour.instance.gameObject);
            SteamVR.enabled = false; // disposes SteamVR

            // Disable UnityXR
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            return Task.CompletedTask;

            // If we need to wait for something specific (like a frame), we use Task.Delay or equivalent.
            // In this case, it seems like you don't need to wait after stopping XR, 
            // so we don't necessarily need an equivalent to 'yield return null' here.
        }

        private static void EnsureXRLoader()
        {
            Type selectedLoaderType = !CheckVR.Instance.forceOpenXr ? typeof(OpenVRLoader) : typeof(OpenXRLoader);

            // dont do anything if we already have the loader selected
            if (XRGeneralSettings.Instance.Manager.activeLoaders.Count > 0 
                && XRGeneralSettings.Instance.Manager.activeLoaders[0].GetType() == selectedLoaderType)
                return;

            XRLoader newLoaderInstance = (XRLoader)ScriptableObject.CreateInstance(selectedLoaderType);
            FieldInfo field = typeof(XRManagerSettings).GetField("m_Loaders", 
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;
            
            // destroy old loaders, set the new laoder
            // this should not happen normally, but changing loader during runtime sounds funni
            if (field.GetValue(XRGeneralSettings.Instance.Manager) is List<XRLoader> currentLoaders)
                foreach (XRLoader loader in currentLoaders.Where(loader => loader != null)) Object.Destroy(loader);

            field.SetValue(XRGeneralSettings.Instance.Manager, new List<XRLoader> { newLoaderInstance });
        }
    }
}
#endif