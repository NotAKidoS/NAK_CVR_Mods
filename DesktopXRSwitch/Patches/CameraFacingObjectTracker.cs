using ABI_RC.Core.Util.Object_Behaviour;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class CameraFacingObjectTracker : MonoBehaviour
{
    public CameraFacingObject cameraFacingObject;
    void Start()
    {
        cameraFacingObject = GetComponent<CameraFacingObject>();
        XRModeSwitchTracker.OnPostXRModeSwitch += PostXRModeSwitch;
    }

    void OnDestroy()
    {
        XRModeSwitchTracker.OnPostXRModeSwitch -= PostXRModeSwitch;
    }

    public void PostXRModeSwitch(bool isXR, Camera activeCamera)
    {
        cameraFacingObject.m_Camera = activeCamera;
    }
}