using ABI.CCK.Components;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandlerHalfBody : IKHandler
{
    public IKHandlerHalfBody(VRIK vrik)
    {
        _vrik = vrik;
        _solver = vrik.solver;
    }

    #region Game Overrides

    public override void OnInitializeIk()
    {
        // Default tracking for HalfBody
        shouldTrackHead = true;
        shouldTrackLeftArm = true;
        shouldTrackRightArm = true;
        shouldTrackLeftLeg = false;
        shouldTrackRightLeg = false;
        shouldTrackPelvis = false;
        shouldTrackLocomotion = true;
    }

    public override void OnPlayerScaled(float scaleDifference)
    {
        VRIKUtils.ApplyScaleToVRIK
        (
            _vrik,
            _locomotionData,
            _scaleDifference = scaleDifference
        );
    }

    public override void OnPlayerHandleMovementParent(CVRMovementParent currentParent)
    {
        // Get current position
        Vector3 currentPosition = currentParent._referencePoint.position;
        Quaternion currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

        // Convert to delta position (how much changed since last frame)
        Vector3 deltaPosition = currentPosition - _movementPosition;
        Quaternion deltaRotation = Quaternion.Inverse(_movementRotation) * currentRotation;

        // Desktop pivots from playerlocal transform
        Vector3 platformPivot = IKManager.Instance.transform.position;

        // Prevent targeting other parent position
        if (_movementParent == currentParent)
        {
            _solver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);
            _ikSimulatedRootAngle = Mathf.Repeat(_ikSimulatedRootAngle + deltaRotation.eulerAngles.y, 360f);
        }

        // Store for next frame
        _movementParent = currentParent;
        _movementPosition = currentPosition;
        _movementRotation = currentRotation;
    }

    #endregion
}