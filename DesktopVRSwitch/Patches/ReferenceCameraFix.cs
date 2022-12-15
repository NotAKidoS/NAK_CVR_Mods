using ABI.CCK.Components;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using Aura2API;
using BeautifyEffect;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

namespace DesktopVRSwitch.Patches;

[HarmonyPatch]
internal class ReferenceCameraFix
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), "SetDefaultCamValues")]
    private static void CVRWorld_SetDefaultCamValues_Postfix(ref CVRWorld __instance)
    {
        Camera active;
        Camera nonActive;
        if (MetaPort.Instance.isUsingVr)
        {
            active = PlayerSetup.Instance.vrCamera.GetComponent<Camera>();
            nonActive = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
        }
        else
        {
            active = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
            nonActive = PlayerSetup.Instance.vrCamera.GetComponent<Camera>();
        }
        CopyActiveCamera(active, nonActive);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), "CopyRefCamValues")]
    private static void CVRWorld_CopyRefCamValues_Postfix(ref CVRWorld __instance)
    {
        Camera active;
        Camera nonActive;
        if (MetaPort.Instance.isUsingVr)
        {
            active = PlayerSetup.Instance.vrCamera.GetComponent<Camera>();
            nonActive = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
        }
        else
        {
            active = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
            nonActive = PlayerSetup.Instance.vrCamera.GetComponent<Camera>();
        }
        CopyActiveCamera(active, nonActive);
    }

    private static void CopyActiveCamera(Camera activeCamera, Camera inactiveCamera)
    {
        //steal basic settings
        inactiveCamera.farClipPlane = activeCamera.farClipPlane;
        inactiveCamera.nearClipPlane = activeCamera.nearClipPlane;
        inactiveCamera.cullingMask = activeCamera.cullingMask;
        inactiveCamera.depthTextureMode = activeCamera.depthTextureMode;

        //steal post processing if added
        PostProcessLayer ppLayerActive = activeCamera.GetComponent<PostProcessLayer>();
        PostProcessLayer ppLayerNonActive = inactiveCamera.GetComponent<PostProcessLayer>();
        if (ppLayerActive != null && ppLayerNonActive != null)
        {
            ppLayerNonActive.enabled = ppLayerActive.enabled;
            ppLayerNonActive.volumeLayer = ppLayerActive.volumeLayer;
        }

        //what even is this aura camera stuff
        AuraCamera auraActive = activeCamera.GetComponent<AuraCamera>();
        AuraCamera auraNonActive = inactiveCamera.AddComponentIfMissing<AuraCamera>();
        if (auraActive != null && auraNonActive != null)
        {
            auraNonActive.enabled = auraActive.enabled;
            auraNonActive.frustumSettings = auraActive.frustumSettings;
        }
        else
        {
            auraNonActive.enabled = false;
        }

        //flare layer thing? the sun :_:_:_:_:_:
        FlareLayer flareActive = activeCamera.GetComponent<FlareLayer>();
        FlareLayer flareNonActive = inactiveCamera.AddComponentIfMissing<FlareLayer>();
        if (flareActive != null && flareNonActive != null)
        {
            flareNonActive.enabled = flareActive.enabled;
        }
        else
        {
            flareNonActive.enabled = false;
        }

        //set correct farclipplane so effect is correct on switching world after switch
        PlayerSetup.Instance.transitionEffectVr.material.SetFloat("ClippingPlane", activeCamera.farClipPlane);
        PlayerSetup.Instance.transitionEffectDesktop.material.SetFloat("ClippingPlane", activeCamera.farClipPlane);

        //and now what the fuck is fog scattering
        AzureFogScattering azureFogActive = activeCamera.GetComponent<AzureFogScattering>();
        AzureFogScattering azureFogNonActive = inactiveCamera.AddComponentIfMissing<AzureFogScattering>();
        if (azureFogActive != null && azureFogNonActive != null)
        {
            azureFogNonActive.fogScatteringMaterial = azureFogActive.fogScatteringMaterial;
        }
        else
        {
            Object.Destroy(inactiveCamera.GetComponent<AzureFogScattering>());
        }

        //why is there so many thingsssssssss
        Beautify beautifyActive = activeCamera.GetComponent<Beautify>();
        Beautify beautifyNonActive = inactiveCamera.AddComponentIfMissing<Beautify>();
        if (beautifyActive != null && beautifyNonActive != null)
        {
            beautifyNonActive.quality = beautifyActive.quality;
            beautifyNonActive.profile = beautifyActive.profile;
        }
        else
        {
            Object.Destroy(inactiveCamera.gameObject.GetComponent<Beautify>());
        }
    }
}