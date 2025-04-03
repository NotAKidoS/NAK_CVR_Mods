using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using Aura2API;
using BeautifyEffect;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;

namespace NAK.DesktopVRSwitch.Patches;

internal static class ReferenceCameraPatch
{
    public static void OnWorldLoad()
    {
        Camera activeCamera = (MetaPort.Instance.isUsingVr ? PlayerSetup.Instance.vrCamera : PlayerSetup.Instance.desktopCamera).GetComponent<Camera>();
        Camera inactiveCamera = (MetaPort.Instance.isUsingVr ? PlayerSetup.Instance.desktopCamera : PlayerSetup.Instance.vrCamera).GetComponent<Camera>();
        CopyToInactiveCam(activeCamera, inactiveCamera);
    }

    private static void CopyToInactiveCam(Camera activeCam, Camera inactiveCam)
    {
        if (inactiveCam == null || activeCam == null)
            return;

        DesktopVRSwitch.Logger.Msg("Copying active camera settings & components to inactive camera.");

        // Copy basic settings
        inactiveCam.farClipPlane = activeCam.farClipPlane;
        inactiveCam.nearClipPlane = activeCam.nearClipPlane;
        inactiveCam.depthTextureMode = activeCam.depthTextureMode;

        // We cant copy this because we set it to 0 with ThirdPerson
        var cullingMask = inactiveCam.cullingMask;
        cullingMask &= -32769;
        cullingMask |= 256;
        cullingMask |= 512;
        cullingMask |= 32;
        cullingMask &= -4097;
        cullingMask |= 1024;
        cullingMask |= 8192;
        inactiveCam.cullingMask = cullingMask;

        // Copy post processing if added
        PostProcessLayer ppLayerActiveCam = activeCam.GetComponent<PostProcessLayer>();
        PostProcessLayer ppLayerInactiveCam = inactiveCam.AddComponentIfMissing<PostProcessLayer>();
        if (ppLayerActiveCam != null && ppLayerInactiveCam != null)
        {
            ppLayerInactiveCam.enabled = ppLayerActiveCam.enabled;
            ppLayerInactiveCam.volumeLayer = ppLayerActiveCam.volumeLayer;
        }

        // Copy Aura camera settings
        AuraCamera auraActiveCam = activeCam.GetComponent<AuraCamera>();
        AuraCamera auraInactiveCam = inactiveCam.AddComponentIfMissing<AuraCamera>();
        if (auraActiveCam != null && auraInactiveCam != null)
        {
            auraInactiveCam.enabled = auraActiveCam.enabled;
            auraInactiveCam.frustumSettings = auraActiveCam.frustumSettings;
        }
        else
        {
            auraInactiveCam.enabled = false;
        }

        // Copy Flare layer settings
        FlareLayer flareActiveCam = activeCam.GetComponent<FlareLayer>();
        FlareLayer flareInactiveCam = inactiveCam.AddComponentIfMissing<FlareLayer>();
        if (flareActiveCam != null && flareInactiveCam != null)
        {
            flareInactiveCam.enabled = flareActiveCam.enabled;
        }
        else
        {
            flareInactiveCam.enabled = false;
        }

        // Copy Azure Fog Scattering settings
        AzureFogScattering azureFogActiveCam = activeCam.GetComponent<AzureFogScattering>();
        AzureFogScattering azureFogInactiveCam = inactiveCam.AddComponentIfMissing<AzureFogScattering>();
        if (azureFogActiveCam != null && azureFogInactiveCam != null)
        {
            azureFogInactiveCam.fogScatteringMaterial = azureFogActiveCam.fogScatteringMaterial;
        }
        else
        {
            UnityEngine.Object.Destroy(inactiveCam.GetComponent<AzureFogScattering>());
        }

        // Copy Beautify settings
        Beautify beautifyActiveCam = activeCam.GetComponent<Beautify>();
        Beautify beautifyInactiveCam = inactiveCam.AddComponentIfMissing<Beautify>();
        if (beautifyActiveCam != null && beautifyInactiveCam != null)
        {
            beautifyInactiveCam.quality = beautifyActiveCam.quality;
            beautifyInactiveCam.profile = beautifyActiveCam.profile;
        }
        else
        {
            UnityEngine.Object.Destroy(inactiveCam.gameObject.GetComponent<Beautify>());
        }
    }
}