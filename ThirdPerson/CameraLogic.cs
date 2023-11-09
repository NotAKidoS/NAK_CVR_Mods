using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using Aura2API;
using BeautifyEffect;
using System.Collections;
using System.Reflection;
using ABI_RC.Core;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;

namespace NAK.ThirdPerson;

internal static class CameraLogic
{
    private static float _dist;
    private static float _scale = 1f;
    private static Camera _thirdpersonCam;
    private static Camera _desktopCam;
    private static int _storedCamMask;
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
            if (_state) _storedCamMask = _desktopCam.cullingMask;
            _desktopCam.cullingMask = _state ? 0 : _storedCamMask;
            _thirdpersonCam.gameObject.SetActive(_state);
        }
    }

    private static bool _setupPostProcessing;
    private static readonly FieldInfo _ppResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_Resources", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _ppOldResourcesFieldInfo = typeof(PostProcessLayer).GetField("m_OldResources", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static IEnumerator SetupCamera()
    {
        yield return new WaitUntil(() => PlayerSetup.Instance);

        _thirdpersonCam = new GameObject("ThirdPersonCameraObj", typeof(Camera)).GetComponent<Camera>();

        _cameraFovClone = _thirdpersonCam.gameObject.AddComponent<CameraFovClone>();

        _desktopCam = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
        _cameraFovClone.targetCamera = _desktopCam;

        _thirdpersonCam.transform.SetParent(_desktopCam.transform);

        RelocateCam(CameraLocation.Default);

        _thirdpersonCam.gameObject.SetActive(false);

        ThirdPerson.Logger.Msg("Finished setting up third person camera.");
    }

    internal static void CopyPlayerCamValues()
    {
        Camera ourCamComponent = _thirdpersonCam;
        Camera activePlayerCam = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        if (ourCamComponent == null || activePlayerCam == null) 
            return;
        
        ThirdPerson.Logger.Msg("Copying active camera settings & components.");
        
        CVRTools.CopyToDestCam(_thirdpersonCam, activePlayerCam);
    }

    internal static void RelocateCam(CameraLocation location, bool resetDist = false)
    {
        _thirdpersonCam.transform.rotation = _desktopCam.transform.rotation;
        if (resetDist) ResetDist();
        switch (location)
        {
            case CameraLocation.FrontView:
                _thirdpersonCam.transform.localPosition = new Vector3(0, 0.015f, 1f - _dist) * _scale;
                _thirdpersonCam.transform.localRotation = new Quaternion(0, 180, 0, 0);
                CurrentLocation = CameraLocation.FrontView;
                break;
            case CameraLocation.RightSide:
                _thirdpersonCam.transform.localPosition = new Vector3(0.3f, 0.015f, -1.5f + _dist) * _scale;
                _thirdpersonCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.RightSide;
                break;
            case CameraLocation.LeftSide:
                _thirdpersonCam.transform.localPosition = new Vector3(-0.3f, 0.015f, -1.5f + _dist) * _scale;
                _thirdpersonCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.LeftSide;
                break;
            case CameraLocation.Default:
            default:
                _thirdpersonCam.transform.localPosition = new Vector3(0, 0.015f, -1.5f + _dist) * _scale;
                _thirdpersonCam.transform.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.Default;
                break;
        }
    }


    private static void ResetDist() => _dist = 0;
    internal static void ScrollDist(float sign) { _dist += sign * 0.25f; RelocateCam(CurrentLocation); }
    internal static void AdjustScale(float height) { _scale = height; RelocateCam(CurrentLocation); }
    internal static void CheckVRMode() { if (MetaPort.Instance.isUsingVr) State = false; }
}
