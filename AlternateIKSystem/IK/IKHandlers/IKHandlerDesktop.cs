using ABI.CCK.Components;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandlerDesktop : IKHandler
{
    public IKHandlerDesktop(VRIK vrik)
    {
        _vrik = vrik;
        _solver = vrik.solver;
    }

    #region Overrides

    public override void OnInitializeIk()
    {
        _vrik.onPreSolverUpdate.AddListener(new UnityAction(OnPreSolverUpdate));
    }

    public override void OnUpdate()
    {
        // Reset avatar local position
        _vrik.transform.localPosition = Vector3.zero;
        _vrik.transform.localRotation = Quaternion.identity;

        UpdateWeights();
    }

    public override void OnPlayerScaled(float scaleDifference, VRIKCalibrationData calibrationData)
    {
        VRIKUtils.ApplyScaleToVRIK
        (
            _vrik,
            calibrationData,
            scaleDifference
        );
    }

    public override void OnPlayerHandleMovementParent(CVRMovementParent currentParent)
    {
        // Get current position
        var currentPosition = currentParent._referencePoint.position;
        var currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

        // Convert to delta position (how much changed since last frame)
        var deltaPosition = currentPosition - _movementPosition;
        var deltaRotation = Quaternion.Inverse(_movementRotation) * currentRotation;

        // Desktop pivots from playerlocal transform
        var platformPivot = IKManager.Instance.transform.position;

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

    #region VRIK Solver Events

    private float _ikSimulatedRootAngle = 0f;

    private void OnPreSolverUpdate()
    {
        _solver.plantFeet = ModSettings.EntryPlantFeet.Value;

        // Emulate old VRChat hip movement
        if (ModSettings.EntryBodyLeanWeight.Value > 0)
        {
            float weightedAngle = ModSettings.EntryProneThrusting.Value ? 1f : ModSettings.EntryBodyLeanWeight.Value * _solver.locomotion.weight;
            float angle = IKManager.Instance._desktopCamera.localEulerAngles.x;
            angle = angle > 180 ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, _vrik.transform.right);
            _solver.spine.headRotationOffset *= rotation;
        }

        // Make root heading follow within a set limit
        if (ModSettings.EntryBodyHeadingLimit.Value > 0)
        {
            float weightedAngleLimit = ModSettings.EntryBodyHeadingLimit.Value * _solver.locomotion.weight;
            float deltaAngleRoot = Mathf.DeltaAngle(IKManager.Instance.transform.eulerAngles.y, _ikSimulatedRootAngle);
            float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);

            if (absDeltaAngleRoot > weightedAngleLimit)
            {
                deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, IKManager.Instance.transform.eulerAngles.y, absDeltaAngleRoot - weightedAngleLimit);
            }
             
            _solver.spine.rootHeadingOffset = deltaAngleRoot;

            if (ModSettings.EntryPelvisHeadingWeight.Value > 0)
            {
                _solver.spine.pelvisRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * ModSettings.EntryPelvisHeadingWeight.Value, 0f);
                _solver.spine.chestRotationOffset *= Quaternion.Euler(0f, -deltaAngleRoot * ModSettings.EntryPelvisHeadingWeight.Value, 0f);
            }

            if (ModSettings.EntryChestHeadingWeight.Value > 0)
            {
                _solver.spine.chestRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * ModSettings.EntryChestHeadingWeight.Value, 0f);
            }
        }
    }

    #endregion

    #region Private Methods

    private float _locomotionWeight = 1f;

    private void UpdateWeights()
    {
        // Lerp locomotion weight, lerp to BodyControl.TrackingUpright???
        float targetWeight =
            (BodyControl.TrackingAll && BodyControl.TrackingLocomotion && BodyControl.AvatarUpright > 0.8f)
                ? 1f
                : 0.0f;
        _locomotionWeight = Mathf.Lerp(_locomotionWeight, targetWeight, Time.deltaTime * 20f);

        if (BodyControl.TrackingAll)
        {
            _vrik.enabled = true;
            _solver.IKPositionWeight = BodyControl.TrackingPositionWeight;
            _solver.locomotion.weight = _locomotionWeight;
            _solver.spine.maxRootAngle = BodyControl.TrackingMaxRootAngle;

            BodyControl.SetHeadWeight(_solver.spine, IKManager.lookAtIk, BodyControl.TrackingHead ? 1f : 0f);

            BodyControl.SetArmWeight(_solver.leftArm, BodyControl.TrackingLeftArm && _solver.leftArm.target != null ? 1f : 0f);
            BodyControl.SetArmWeight(_solver.rightArm, BodyControl.TrackingRightArm && _solver.rightArm.target != null ? 1f : 0f);

            BodyControl.SetLegWeight(_solver.leftLeg, BodyControl.TrackingLeftLeg && _solver.leftLeg.target != null ? 1f : 0f);
            BodyControl.SetLegWeight(_solver.rightLeg, BodyControl.TrackingRightLeg && _solver.rightLeg.target != null ? 1f : 0f);

            BodyControl.SetPelvisWeight(_solver.spine, BodyControl.TrackingPelvis && _solver.spine.pelvisTarget != null ? 1f : 0f);
        }
        else
        {
            _vrik.enabled = false;
            _solver.IKPositionWeight = 0f;
            _solver.locomotion.weight = 0f;
            _solver.spine.maxRootAngle = 0f;

            BodyControl.SetHeadWeight(_solver.spine, IKManager.lookAtIk, 0f);
            BodyControl.SetArmWeight(_solver.leftArm, 0f);
            BodyControl.SetArmWeight(_solver.rightArm, 0f);
            BodyControl.SetLegWeight(_solver.leftLeg, 0f);
            BodyControl.SetLegWeight(_solver.rightLeg, 0f);
            BodyControl.SetPelvisWeight(_solver.spine, 0f);
        }

        // Desktop should never have head position weight
        _solver.spine.positionWeight = 0f;

        // Avatar Motion Tweaker uses this hack, so we must reset it
        _solver.leftLeg.useAnimatedBendNormal = false;
        _solver.rightLeg.useAnimatedBendNormal = false;
    }

    #endregion
}