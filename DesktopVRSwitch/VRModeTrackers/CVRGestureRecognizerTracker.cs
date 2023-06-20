using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRGestureRecognizerTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPostSwitch(bool intoVR)
    {
        CVRGestureRecognizer _cvrGestureRecognizer = CVRGestureRecognizer.Instance;
        if (_cvrGestureRecognizer == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVRGestureRecognizer!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Updating CVRGestureRecognizer _camera to active camera.");

        _cvrGestureRecognizer._camera = Utils.GetPlayerCameraObject(intoVR).GetComponent<Camera>();
    }
}