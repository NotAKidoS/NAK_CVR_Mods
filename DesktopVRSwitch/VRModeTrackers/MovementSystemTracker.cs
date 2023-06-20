using ABI_RC.Systems.MovementSystem;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class MovementSystemTracker : VRModeTracker
{
    private MovementSystem _movementSystem;
    private Vector3 preSwitchWorldPosition;
    private Quaternion preSwitchWorldRotation;

    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPreVRModeSwitch += OnPreSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPreVRModeSwitch -= OnPreSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPreSwitch(bool intoVR)
    {
        _movementSystem = MovementSystem.Instance;

        Vector3 position = _movementSystem.rotationPivot.transform.position;
        position.y = _movementSystem.transform.position.y;
        preSwitchWorldPosition = position;
        preSwitchWorldRotation = _movementSystem.rotationPivot.transform.rotation;

        _movementSystem.ChangeCrouch(false);
        _movementSystem.ChangeProne(false);
    }

    private void OnPostSwitch(bool intoVR)
    {
        _movementSystem.rotationPivot = Utils.GetPlayerCameraObject(intoVR).transform;
        _movementSystem.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);

        if (!intoVR)
            _movementSystem.UpdateColliderCenter(_movementSystem.transform.position);

        _movementSystem.ChangeCrouch(false);
        _movementSystem.ChangeProne(false);
    }
}