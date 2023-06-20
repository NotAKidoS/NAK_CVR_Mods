using ABI_RC.Core.Util.Object_Behaviour;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CameraFacingObjectTracker : MonoBehaviour
{
    internal CameraFacingObject _cameraFacingObject;

    void Start()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    void OnDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    public void OnPostSwitch(bool intoVR)
    {
        _cameraFacingObject.m_Camera = Utils.GetPlayerCameraObject(intoVR).GetComponent<Camera>();
    }
}