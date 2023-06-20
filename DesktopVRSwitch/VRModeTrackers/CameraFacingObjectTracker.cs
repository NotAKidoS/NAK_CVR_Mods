using ABI_RC.Core.Util.Object_Behaviour;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CameraFacingObjectTracker : MonoBehaviour
{
    private CameraFacingObject _cameraFacingObject;

    public CameraFacingObjectTracker(CameraFacingObject cameraFacingObject)
    {
        this._cameraFacingObject = cameraFacingObject;
    }

    private void OnDestroy()
    {
    }

    public void OnPreSwitch(bool intoVR) { }

    public void OnFailedSwitch(bool intoVR) { }

    public void OnPostSwitch(bool intoVR)
    {
        _cameraFacingObject.m_Camera = Utils.GetPlayerCameraObject(intoVR).GetComponent<Camera>();
    }
}