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
    private static GameObject _ourCam;
    private static GameObject _desktopCam;
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
            _ourCam.SetActive(_state);
        }
    }

    private static bool _setupPostProcessing;
    private static readonly FieldInfo _ppResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_Resources", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _ppOldResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_OldResources", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static IEnumerator SetupCamera()
    {
        yield return new WaitUntil(() => PlayerSetup.Instance);

        _ourCam = new GameObject("ThirdPersonCameraObj") { };
        _ourCam.AddComponent<Camera>();

        _cameraFovClone = _ourCam.AddComponent<CameraFovClone>();

        _desktopCam = PlayerSetup.Instance.desktopCamera;
        _cameraFovClone.targetCamera = _desktopCam.GetComponent<Camera>();

        _ourCam.transform.SetParent(_desktopCam.transform);

        RelocateCam(CameraLocation.Default);

        _ourCam.gameObject.SetActive(false);

        ThirdPerson.Logger.Msg("Finished setting up third person camera.");
    }

    internal static void CopyFromPlayerCam()
    {
        Camera ourCamComponent = _ourCam.GetComponent<Camera>();
        Camera playerCamComponent = _desktopCam.GetComponent<Camera>();
        if (ourCamComponent == null || playerCamComponent == null) return;
        ThirdPerson.Logger.Msg("Copying active camera settings & components.");

        //steal basic settings
        ourCamComponent.farClipPlane = playerCamComponent.farClipPlane;
        ourCamComponent.nearClipPlane = playerCamComponent.nearClipPlane;
        ourCamComponent.cullingMask = playerCamComponent.cullingMask;
        ourCamComponent.depthTextureMode = playerCamComponent.depthTextureMode;

        //steal post processing if added
        PostProcessLayer ppLayerPlayerCam = playerCamComponent.GetComponent<PostProcessLayer>();
        PostProcessLayer ppLayerThirdPerson = ourCamComponent.AddComponentIfMissing<PostProcessLayer>();
        if (ppLayerPlayerCam != null && ppLayerThirdPerson != null)
        {
            ppLayerThirdPerson.enabled = ppLayerPlayerCam.enabled;
            ppLayerThirdPerson.volumeLayer = ppLayerPlayerCam.volumeLayer;
            //need to copy these via reflection, otherwise post processing will error
            if (!_setupPostProcessing)
            {
                _setupPostProcessing = true;
                PostProcessResources resources = (PostProcessResources)_ppResourcesFieldInfo.GetValue(ppLayerPlayerCam);
                PostProcessResources oldResources = (PostProcessResources)_ppOldResourcesFieldInfo.GetValue(ppLayerPlayerCam);
                _ppResourcesFieldInfo.SetValue(ppLayerThirdPerson, resources);
                _ppResourcesFieldInfo.SetValue(ppLayerThirdPerson, oldResources);
            }
        }

        //what even is this aura camera stuff
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

        //flare layer thing? the sun :_:_:_:_:_:
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

        //and now what the fuck is fog scattering
        AzureFogScattering azureFogPlayerCam = playerCamComponent.GetComponent<AzureFogScattering>();
        AzureFogScattering azureFogThirdPerson = ourCamComponent.AddComponentIfMissing<AzureFogScattering>();
        if (azureFogPlayerCam != null && azureFogThirdPerson != null)
        {
            azureFogThirdPerson.fogScatteringMaterial = azureFogPlayerCam.fogScatteringMaterial;
        }
        else
        {
            Object.Destroy(ourCamComponent.GetComponent<AzureFogScattering>());
        }

        //why is there so many thingsssssssss
        Beautify beautifyPlayerCam = playerCamComponent.GetComponent<Beautify>();
        Beautify beautifyThirdPerson = ourCamComponent.AddComponentIfMissing<Beautify>();
        if (beautifyPlayerCam != null && beautifyThirdPerson != null)
        {
            beautifyThirdPerson.quality = beautifyPlayerCam.quality;
            beautifyThirdPerson.profile = beautifyPlayerCam.profile;
        }
        else
        {
            Object.Destroy(ourCamComponent.gameObject.GetComponent<Beautify>());
        }
    }

    internal static void RelocateCam(CameraLocation location, bool resetDist = false)
    {
        _ourCam.transform.rotation = _desktopCam.transform.rotation;
        if (resetDist) ResetDist();
        switch (location)
        {
            case CameraLocation.FrontView:
                _ourCam.transform.localPosition = new Vector3(0, 0.015f, 0.55f - _dist);
                _ourCam.transform.localRotation = new Quaternion(0, 180, 0, 0);
                CurrentLocation = CameraLocation.FrontView;
                break;
            case CameraLocation.RightSide:
                _ourCam.transform.localPosition = new Vector3(0.3f, 0.015f, -0.55f + _dist);
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.RightSide;
                break;
            case CameraLocation.LeftSide:
                _ourCam.transform.localPosition = new Vector3(-0.3f, 0.015f, -0.55f + _dist);
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.LeftSide;
                break;
            case CameraLocation.Default:
            default:
                _ourCam.transform.localPosition = new Vector3(0, 0.015f, -0.55f + _dist);
                _ourCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.Default;
                break;
        }
    }

    private static void ResetDist() => _dist = 0;
    internal static void IncrementDist() { _dist += 0.25f; RelocateCam(CurrentLocation); }
    internal static void DecrementDist() { _dist -= 0.25f; RelocateCam(CurrentLocation); }
}