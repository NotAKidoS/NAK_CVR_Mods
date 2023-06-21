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
        DesktopVRSwitch.Logger.Msg("Configuring CVRWorld. Updating PostProcessing & DesktopCameraController FOV settings.");

        // some post processing settings aren't used in VR
        CVRWorld.Instance.UpdatePostProcessing();
        UpdateCVRDesktopCameraController();
    }
    
    private void UpdateCVRDesktopCameraController()
    {
        // Just making sure- Starting in VR will not call Start() as rig is disabled
        if (CVR_DesktopCameraController._cam == null)
            CVR_DesktopCameraController._cam = PlayerSetup.Instance.desktopCamera.GetComponent<Camera>();

        CVR_DesktopCameraController.defaultFov = Mathf.Clamp(CVRWorld.Instance.fov, 60f, 120f);
        CVR_DesktopCameraController.zoomFov = CVR_DesktopCameraController.defaultFov * 0.5f;
        CVR_DesktopCameraController.enableZoom = CVRWorld.Instance.enableZoom;

        // must happen after PlayerSetupTracker
        CVR_DesktopCameraController.UpdateFov();

        CVR_MenuManager.Instance.coreData.instance.current_game_rule_no_zoom = !CVRWorld.Instance.enableZoom;

        // UICamera has a script that copies the FOV from the desktop cam.
        // Toggling the cameras on/off resets the aspect ratio,
        // so when rigs switch, that is already handled.
    }
}