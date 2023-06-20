using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRWorldTracker : VRModeTracker
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
        CVRWorld _cvrWorld = CVRWorld.Instance;
        if (_cvrWorld == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting CVRWorld!");
            return;
        }
        DesktopVRSwitch.Logger.Msg("Configuring CVRWorld. Updating PostProcessing & DesktopCameraController FOV settings.");

        // some post processing settings aren't used in VR
        _cvrWorld.UpdatePostProcessing();
        UpdateCVRDesktopCameraController(_cvrWorld);
    }

    private void UpdateCVRDesktopCameraController(CVRWorld _cvrWorld)
    {
        // Just making sure- Starting in VR will not call Start() as rig is disabled
        if (CVR_DesktopCameraController._cam == null)
            CVR_DesktopCameraController._cam = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();

        CVR_DesktopCameraController.defaultFov = Mathf.Clamp(_cvrWorld.fov, 60f, 120f);
        CVR_DesktopCameraController.zoomFov = CVR_DesktopCameraController.defaultFov * 0.5f;
        CVR_DesktopCameraController.enableZoom = _cvrWorld.enableZoom;
        CVR_DesktopCameraController.UpdateFov(); // must happen after PlayerSetupTracker
        CVR_MenuManager.Instance.coreData.instance.current_game_rule_no_zoom = !_cvrWorld.enableZoom;

        // UICamera has a script that copies the FOV from the desktop cam.
        // Toggling the cameras on/off resets the aspect ratio,
        // so when rigs switch, that is already handled.
    }
}