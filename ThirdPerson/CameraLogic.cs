using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util.Object_Behaviour;
using Aura2API;
using BeautifyEffect;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;

namespace NAK.ThirdPerson;

internal static class CameraLogic
{
    private static float _dist;
    private static float _scale = 1f;
    private static Camera _ourCam;
    private static Camera _desktopCam;
    private static int _desktopCamMask;
    private static CameraFovClone _cameraFovClone;

    internal static CameraLocation CurrentLocation = CameraLocation.Default;

    internal enum CameraLocation
    {
        Default,
        FrontView,
        RightSide,
        LeftSide
    }

    private static bool _state;
    internal static bool State
    {
        get => _state;
        set
        {
            _state = value;
            _desktopCam.cullingMask = _state ? 0 : _desktopCamMask;
            _ourCam.gameObject.SetActive(_state);
        }
    }

    static bool _setupPostProcessing;
    static readonly FieldInfo _ppResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_Resources", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo _ppOldResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_OldResources", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static IEnumerator SetupCamera()
    {
        yield return new WaitUntil(() => PlayerSetup.Instance);

        _ourCam = new GameObject("ThirdPersonCameraObj", typeof(Camera)).GetComponent<Camera>();

        _cameraFovClone = _ourCam.gameObject.AddComponent<CameraFovClone>();

        _desktopCam = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
        _desktopCamMask = _desktopCam.cullingMask;
        _cameraFovClone.targetCamera = _desktopCam;

        _ourCam.transform.SetParent(_desktopCam.transform);

        RelocateCam(CameraLocation.Default);

        _ourCam.gameObject.SetActive(false);

        ThirdPerson.Logger.Msg("Finished setting up third person camera.");
    }

    internal static void CopyPlayerCamValues()
    {
        Camera ourCamComponent = _ourCam.GetComponent<Camera>();
        Camera playerCamComponent = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        if (ourCamComponent == null || playerCamComponent == null) return;
        ThirdPerson.Logger.Msg("Copying active camera settings & components.");

        // Copy basic settings
        ourCamComponent.farClipPlane = playerCamComponent.farClipPlane;
        ourCamComponent.nearClipPlane = playerCamComponent.nearClipPlane;
        ourCamComponent.depthTextureMode = playerCamComponent.depthTextureMode;

        // We cant copy this because we set it to 0
        ourCamComponent.cullingMask &= -32769;
        ourCamComponent.cullingMask |= 256;
        ourCamComponent.cullingMask |= 512;
        ourCamComponent.cullingMask |= 32;
        ourCamComponent.cullingMask &= -4097;
        ourCamComponent.cullingMask |= 1024;
        ourCamComponent.cullingMask |= 8192;

        // Copy post processing if added
        PostProcessLayer ppLayerPlayerCam = playerCamComponent.GetComponent<PostProcessLayer>();
        PostProcessLayer ppLayerThirdPerson = ourCamComponent.AddComponentIfMissing<PostProcessLayer>();
        if (ppLayerPlayerCam != null && ppLayerThirdPerson != null)
        {
            ppLayerThirdPerson.enabled = ppLayerPlayerCam.enabled;
            ppLayerThirdPerson.volumeLayer = ppLayerPlayerCam.volumeLayer;
            // Copy these via reflection, otherwise post processing will error
            if (!_setupPostProcessing)
            {
                _setupPostProcessing = true;
                PostProcessResources resources = (PostProcessResources)_ppResourcesFieldInfo.GetValue(ppLayerPlayerCam);
                PostProcessResources oldResources = (PostProcessResources)_ppOldResourcesFieldInfo.GetValue(ppLayerPlayerCam);
                _ppResourcesFieldInfo.SetValue(ppLayerThirdPerson, resources);
                _ppResourcesFieldInfo.SetValue(ppLayerThirdPerson, oldResources);
            }
        }

        // Copy Aura camera settings
        AuraCamera auraPlayerCam = playerCamComponent.GetComponent<AuraCamera>();
        AuraCamera auraThirdPerson = ourCamComponent.AddComponentIfMissing<AuraCamera>();
        if (auraPlayerCam != null && auraThirdPerson != null)
        {
            auraThirdPerson.enabled = auraPlayerCam.enabled;
            auraThirdPerson.frustumSettings = auraPlayerCam.frustumSettings;
        }
        else
        {
            auraThirdPerson.enabled = false;
        }

        // Copy Flare layer settings
        FlareLayer flarePlayerCam = playerCamComponent.GetComponent<FlareLayer>();
        FlareLayer flareThirdPerson = ourCamComponent.AddComponentIfMissing<FlareLayer>();
        if (flarePlayerCam != null && flareThirdPerson != null)
        {
            flareThirdPerson.enabled = flarePlayerCam.enabled;
        }
        else
        {
            flareThirdPerson.enabled = false;
        }

        // Copy Azure Fog Scattering settings
        AzureFogScattering azureFogPlayerCam = playerCamComponent.GetComponent<AzureFogScattering>();
        AzureFogScattering azureFogThirdPerson = ourCamComponent.AddComponentIfMissing<AzureFogScattering>();
        if (azureFogPlayerCam != null && azureFogThirdPerson != null)
        {
            azureFogThirdPerson.fogScatteringMaterial = azureFogPlayerCam.fogScatteringMaterial;
        }
        else
        {
            UnityEngine.Object.Destroy(ourCamComponent.GetComponent<AzureFogScattering>());
        }

        // Copy Beautify settings
        Beautify beautifyPlayerCam = playerCamComponent.GetComponent<Beautify>();
        Beautify beautifyThirdPerson = ourCamComponent.AddComponentIfMissing<Beautify>();
        if (beautifyPlayerCam != null && beautifyThirdPerson != null)
        {
            beautifyThirdPerson.quality = beautifyPlayerCam.quality;
            beautifyThirdPerson.profile = beautifyPlayerCam.profile;
        }
        else
        {
            UnityEngine.Object.Destroy(ourCamComponent.gameObject.GetComponent<Beautify>());
        }
    }

    internal static void RelocateCam(CameraLocation location, bool resetDist = false)
    {
        _ourCam.transform.rotation = _desktopCam.transform.rotation;
        if (resetDist) ResetDist();
        switch (location)
        {
            case CameraLocation.FrontView:
                _ourCam.transform.localPosition = new Vector3(0, 0.015f, 1f - _dist) * _scale;
                _ourCam.transform.localRotation = new Quaternion(0, 180, 0, 0);
                CurrentLocation = CameraLocation.FrontView;
                break;
            case CameraLocation.RightSide:
                _ourCam.transform.localPosition = new Vector3(0.3f, 0.015f, -1.5f + _dist) * _scale;
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.RightSide;
                break;
            case CameraLocation.LeftSide:
                _ourCam.transform.localPosition = new Vector3(-0.3f, 0.015f, -1.5f + _dist) * _scale;
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.LeftSide;
                break;
            case CameraLocation.Default:
            default:
                _ourCam.transform.localPosition = new Vector3(0, 0.015f, -1.5f + _dist) * _scale;
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.Default;
                break;
        }
    }

    private static void ResetDist() => _dist = 0;
    internal static void ScrollDist(float sign) { _dist += sign * 0.25f; RelocateCam(CurrentLocation); }
    internal static void AdjustScale(float height) { _scale = height; RelocateCam(CurrentLocation); }
}
