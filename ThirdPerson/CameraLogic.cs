using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using System.Collections;
using ABI_RC.Core;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.ThirdPerson;

internal static class CameraLogic
{
    private static float _dist;
    private static float _scale = 1f;
    private static Camera _thirdPersonCam;
    private static Camera _uiCam;
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
            if (_state == value) return;
            _state = !CheckIsRestricted() && value;
            
            if (_state) _storedCamMask = _desktopCam.cullingMask;
            _desktopCam.cullingMask = _state ? 0 : _storedCamMask;
            _uiCam.cullingMask = _state ? _uiCam.cullingMask & ~(1 << CVRLayers.PlayerClone) : _uiCam.cullingMask | (1 << CVRLayers.PlayerClone);
            _thirdPersonCam.gameObject.SetActive(_state);
        }
    }
    
    internal static IEnumerator SetupCamera()
    {
        yield return new WaitUntil(() => PlayerSetup.Instance);
        
        _thirdPersonCam = new GameObject("ThirdPersonCameraObj", typeof(Camera)).GetComponent<Camera>();
        
        _cameraFovClone = _thirdPersonCam.gameObject.AddComponent<CameraFovClone>();

        _desktopCam = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();
        _cameraFovClone.targetCamera = _desktopCam;

        _thirdPersonCam.transform.SetParent(_desktopCam.transform);
        _uiCam = _desktopCam.transform.Find("_UICamera").GetComponent<Camera>();

        RelocateCam(CameraLocation.Default);

        _thirdPersonCam.gameObject.SetActive(false);
        
        ThirdPerson.Logger.Msg("Finished setting up third person camera.");
    }

    internal static void CopyPlayerCamValues()
    {
        Camera activePlayerCam = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        if (_thirdPersonCam == null || activePlayerCam == null) 
            return;
        
        ThirdPerson.Logger.Msg("Copying active camera settings & components.");
        CVRTools.CopyToDestCam(activePlayerCam, _thirdPersonCam, true);

        if (!CheckIsRestricted()) 
            return;
        
        ThirdPerson.Logger.Msg("Third person camera is restricted by the world.");
        State = false;
    }

    internal static void RelocateCam(CameraLocation location, bool resetDist = false)
    {
        Transform thirdPersonCam = _thirdPersonCam.transform;
        thirdPersonCam.rotation = _desktopCam.transform.rotation;
        if (resetDist) ResetDist();
        switch (location)
        {
            case CameraLocation.FrontView:
                thirdPersonCam.localPosition = new Vector3(0, 0.015f, 1f - _dist) * _scale;
                thirdPersonCam.localRotation = new Quaternion(0, 180, 0, 0);
                CurrentLocation = CameraLocation.FrontView;
                break;
            case CameraLocation.RightSide:
                thirdPersonCam.localPosition = new Vector3(0.3f, 0.015f, -1.5f + _dist) * _scale;
                thirdPersonCam.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.RightSide;
                break;
            case CameraLocation.LeftSide:
                thirdPersonCam.localPosition = new Vector3(-0.3f, 0.015f, -1.5f + _dist) * _scale;
                thirdPersonCam.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.LeftSide;
                break;
            case CameraLocation.Default:
            default:
                thirdPersonCam.localPosition = new Vector3(0, 0.015f, -1.5f + _dist) * _scale;
                thirdPersonCam.localRotation = new Quaternion(0, 0, 0, 0);
                CurrentLocation = CameraLocation.Default;
                break;
        }
    }
    
    private static void ResetDist() => _dist = 0;
    internal static void ScrollDist(float sign) { _dist += sign * 0.25f; RelocateCam(CurrentLocation); }
    internal static void AdjustScale(float height) { _scale = height; RelocateCam(CurrentLocation); }
    internal static void CheckVRMode() { if (MetaPort.Instance.isUsingVr) State = false; }

    private static bool CheckIsRestricted()
        => CVRWorld.Instance != null && !CVRWorld.Instance.enableZoom;
}