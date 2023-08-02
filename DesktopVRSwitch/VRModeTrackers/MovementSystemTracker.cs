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

    private void OnPreSwitch(bool intoVR)
    {

        DesktopVRSwitch.Logger.Msg("Storing player world position and rotation.");

        var pivotTransform = MovementSystem.Instance.rotationPivot.transform;
        preSwitchWorldPosition = pivotTransform.position;
        preSwitchWorldPosition.y = MovementSystem.Instance.transform.position.y;
        preSwitchWorldRotation = pivotTransform.rotation;

        MovementSystem.Instance.ChangeCrouch(false);
        MovementSystem.Instance.ChangeProne(false);
        MovementSystem.Instance.SetImmobilized(true);

    }

    private void OnFailedSwitch(bool intoVR)
    {
        DesktopVRSwitch.Logger.Msg("Resetting MovementSystem mobility.");

        MovementSystem.Instance.SetImmobilized(false);
    }

    private void OnPostSwitch(bool intoVR)
    {
        // Lazy
        MelonLoader.MelonCoroutines.Start(TeleportFrameAfter(intoVR));
    }

    private IEnumerator TeleportFrameAfter(bool intoVR)
    {
        yield return null; // need to wait a frame

        DesktopVRSwitch.Logger.Msg("Resetting MovementSystem mobility and applying stored position and rotation.");

        MovementSystem.Instance.rotationPivot = Utils.GetPlayerCameraObject(intoVR).transform;
        MovementSystem.Instance.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);

        if (!intoVR)
            MovementSystem.Instance.UpdateColliderCenter(MovementSystem.Instance.transform.position);

        MovementSystem.Instance.ChangeCrouch(false);
        MovementSystem.Instance.ChangeProne(false);
        MovementSystem.Instance.SetImmobilized(false);

        yield break;
    }
}