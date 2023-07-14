using ABI.CCK.Components;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandlerDesktop : IKHandler
{
    public IKHandlerDesktop(VRIK vrik)
    {
        _vrik = vrik;
        _solver = vrik.solver;
    }

    #region Game Overrides

    public override void OnInitializeIk()
    {
        // Default tracking for Desktop
        shouldTrackHead = true;
        shouldTrackLeftArm = false;
        shouldTrackRightArm = false;
        shouldTrackLeftLeg = false;
        shouldTrackRightLeg = false;
        shouldTrackPelvis = false;
        shouldTrackLocomotion = true;

        _vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdateDesktop);
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

    #region Weight Overrides

    public override void UpdateWeights()
    {
        // Reset avatar local position
        _vrik.transform.localPosition = Vector3.zero;
        _vrik.transform.localRotation = Quaternion.identity;

        base.UpdateWeights();

        // Desktop should never have head position weight
        _solver.spine.positionWeight = 0f;
    }

    protected override void Update_HeadWeight()
    {
        float targetWeight = GetTargetWeight(BodyControl.TrackingHead, true);
        BodyControl.SetHeadWeight(_solver.spine, targetWeight);
        BodyControl.SetLookAtWeight(IKManager.lookAtIk, targetWeight);
    }

    protected override void Update_LeftArmWeight()
    {
        float leftArmWeight = GetTargetWeight(BodyControl.TrackingLeftArm, _solver.leftArm.target != null);
        BodyControl.SetArmWeight(_solver.leftArm, leftArmWeight);
    }

    protected override void Update_RightArmWeight()
    {
        float rightArmWeight = GetTargetWeight(BodyControl.TrackingRightArm, _solver.rightArm.target != null);
        BodyControl.SetArmWeight(_solver.rightArm, rightArmWeight);
    }

    protected override void Update_LeftLegWeight()
    {
        float leftLegWeight = GetTargetWeight(BodyControl.TrackingLeftLeg, _solver.leftLeg.target != null);
        BodyControl.SetLegWeight(_solver.leftLeg, leftLegWeight);
    }

    protected override void Update_RightLegWeight()
    {
        float rightLegWeight = GetTargetWeight(BodyControl.TrackingRightLeg, _solver.rightLeg.target != null);
        BodyControl.SetLegWeight(_solver.rightLeg, rightLegWeight);
    }

    protected override void Update_PelvisWeight()
    {
        float pelvisWeight = GetTargetWeight(BodyControl.TrackingPelvis, _solver.spine.pelvisTarget != null);
        BodyControl.SetPelvisWeight(_solver.spine, pelvisWeight);
    }

    protected override void Update_LocomotionWeight()
    {
        _locomotionWeight = Mathf.Lerp(_locomotionWeight, BodyControl.TrackingLocomotion ? 1f : 0f,
            Time.deltaTime * ModSettings.EntryIKLerpSpeed.Value * 2f);
        BodyControl.SetLocomotionWeight(_solver.locomotion, _locomotionWeight);
    }

    protected override void Update_IKPositionWeight()
    {
        float ikPositionWeight = BodyControl.TrackingAll ? BodyControl.TrackingIKPositionWeight : 0f;
        BodyControl.SetIKPositionWeight(_solver, ikPositionWeight);
        BodyControl.SetIKPositionWeight(IKManager.lookAtIk, ikPositionWeight);
    }

    #endregion

    #region VRIK Solver Events

    private void OnPreSolverUpdateDesktop()
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
}