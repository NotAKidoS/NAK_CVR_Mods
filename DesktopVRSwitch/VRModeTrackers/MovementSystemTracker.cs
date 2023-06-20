using ABI_RC.Systems.MovementSystem;
using System.Collections;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class MovementSystemTracker : VRModeTracker
{
    private Vector3 preSwitchWorldPosition;
    private Quaternion preSwitchWorldRotation;

    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPreVRModeSwitch += OnPreSwitch;
        VRModeSwitchManager.OnFailVRModeSwitch += OnFailedSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPreVRModeSwitch -= OnPreSwitch;
        VRModeSwitchManager.OnFailVRModeSwitch -= OnFailedSwitch;
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private MovementSystem GetMovementSystemInstance()
    {
        MovementSystem _movementSystem = MovementSystem.Instance;
        if (_movementSystem == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting MovementSystem!");
        }
        return _movementSystem;
    }

    private void OnPreSwitch(bool intoVR)
    {
        MovementSystem _movementSystem = GetMovementSystemInstance();
        if (_movementSystem != null)
        {
            DesktopVRSwitch.Logger.Msg("Storing player world position and rotation.");
            preSwitchWorldPosition = _movementSystem.rotationPivot.transform.position;
            preSwitchWorldPosition.y = _movementSystem.transform.position.y;
            preSwitchWorldRotation = _movementSystem.rotationPivot.transform.rotation;

            _movementSystem.ChangeCrouch(false);
            _movementSystem.ChangeProne(false);
            _movementSystem.SetImmobilized(true);
        }
    }

    private void OnFailedSwitch(bool intoVR)
    {
        MovementSystem _movementSystem = GetMovementSystemInstance();
        if (_movementSystem != null)
        {
            DesktopVRSwitch.Logger.Msg("Resetting MovementSystem mobility.");
            _movementSystem.SetImmobilized(false);
        }
    }

    private void OnPostSwitch(bool intoVR)
    {
        // Lazy
        MelonLoader.MelonCoroutines.Start(TeleportFrameAfter(intoVR));
    }

    private IEnumerator TeleportFrameAfter(bool intoVR)
    {
        yield return null; // need to wait a frame

        MovementSystem _movementSystem = GetMovementSystemInstance();
        if (_movementSystem != null)
        {
            DesktopVRSwitch.Logger.Msg("Resetting MovementSystem mobility and applying stored position and rotation.");

            _movementSystem.rotationPivot = Utils.GetPlayerCameraObject(intoVR).transform;
            _movementSystem.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);

            if (!intoVR)
                _movementSystem.UpdateColliderCenter(_movementSystem.transform.position);

            _movementSystem.ChangeCrouch(false);
            _movementSystem.ChangeProne(false);
            _movementSystem.SetImmobilized(false);
        }

        yield break;
    }
}