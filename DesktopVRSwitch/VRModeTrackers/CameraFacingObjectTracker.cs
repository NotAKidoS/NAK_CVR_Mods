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

    public void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        _cameraFacingObject.m_Camera = args.PlayerCamera;
    }
}