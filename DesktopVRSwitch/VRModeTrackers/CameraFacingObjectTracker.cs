using ABI_RC.Core.Util.Object_Behaviour;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CameraFacingObjectTracker : MonoBehaviour
{
    private CameraFacingObject _cameraFacingObject;

    private void Start()
    {
        _cameraFacingObject = GetComponent<CameraFacingObject>();
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    private void OnDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    public void OnPostSwitch(bool intoVR)
    {
        // TODO: cache camera
        _cameraFacingObject.m_Camera = Utils.GetPlayerCameraObject(intoVR).GetComponent<Camera>();
    }
}