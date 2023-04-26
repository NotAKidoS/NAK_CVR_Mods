using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using Aura2API;
using BeautifyEffect;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

namespace NAK.DesktopVRSwitch.Patches;

internal class ReferenceCameraPatch
{
    internal static void OnWorldLoad()
    {
        Camera activeCamera = (MetaPort.Instance.isUsingVr ? PlayerSetup.Instance.vrCamera : PlayerSetup.Instance.desktopCamera).GetComponent<Camera>();
        Camera inactiveCamera = (MetaPort.Instance.isUsingVr ? PlayerSetup.Instance.desktopCamera : PlayerSetup.Instance.vrCamera).GetComponent<Camera>();
        CopyToInactiveCam(activeCamera, inactiveCamera);
    }

    internal static void CopyToInactiveCam(Camera activeCam, Camera inactiveCam)
    {
        DesktopVRSwitch.Logger.Msg("Copying active camera settings & components to inactive camera.");

        //steal basic settings
        inactiveCam.farClipPlane = activeCam.farClipPlane;
        inactiveCam.nearClipPlane = activeCam.nearClipPlane;
        inactiveCam.cullingMask = activeCam.cullingMask;
        inactiveCam.depthTextureMode = activeCam.depthTextureMode;

        //steal post processing if added
        PostProcessLayer ppLayerActiveCam = activeCam.GetComponent<PostProcessLayer>();
        PostProcessLayer ppLayerInactiveCam = inactiveCam.AddComponentIfMissing<PostProcessLayer>();
        if (ppLayerActiveCam != null && ppLayerInactiveCam != null)
        {
            ppLayerInactiveCam.enabled = ppLayerActiveCam.enabled;
            ppLayerInactiveCam.volumeLayer = ppLayerActiveCam.volumeLayer;
        }

        //what even is this aura camera stuff
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

        //flare layer thing? the sun :_:_:_:_:_:
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

        //and now what the fuck is fog scattering
        AzureFogScattering azureFogActiveCam = activeCam.GetComponent<AzureFogScattering>();
        AzureFogScattering azureFogInactiveCam = inactiveCam.AddComponentIfMissing<AzureFogScattering>();
        if (azureFogActiveCam != null && azureFogInactiveCam != null)
        {
            azureFogInactiveCam.fogScatteringMaterial = azureFogActiveCam.fogScatteringMaterial;
        }
        else
        {
            Object.Destroy(inactiveCam.GetComponent<AzureFogScattering>());
        }

        //why is there so many thingsssssssss
        Beautify beautifyActiveCam = activeCam.GetComponent<Beautify>();
        Beautify beautifyInactiveCam = inactiveCam.AddComponentIfMissing<Beautify>();
        if (beautifyActiveCam != null && beautifyInactiveCam != null)
        {
            beautifyInactiveCam.quality = beautifyActiveCam.quality;
            beautifyInactiveCam.profile = beautifyActiveCam.profile;
        }
        else
        {
            Object.Destroy(inactiveCam.gameObject.GetComponent<Beautify>());
        }
    }
}