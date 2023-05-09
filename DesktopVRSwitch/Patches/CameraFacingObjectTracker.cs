using ABI_RC.Core.Util.Object_Behaviour;
using UnityEngine;

namespace NAK.DesktopVRSwitch.Patches;

public class CameraFacingObjectTracker : MonoBehaviour
{
    public CameraFacingObject cameraFacingObject;

    void Start()
    {
        cameraFacingObject = GetComponent<CameraFacingObject>();
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }

    void OnDestroy()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PostVRModeSwitch(bool enableVR, Camera activeCamera)
    {
        cameraFacingObject.m_Camera = activeCamera;
    }
}