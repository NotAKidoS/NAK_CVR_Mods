using ABI.CCK.Components;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal abstract class IKHandler
{
    #region Variables

    internal VRIK _vrik;
    internal IKSolverVR _solver;

    internal bool shouldTrackAll = true;
    internal bool shouldTrackHead = true;
    internal bool shouldTrackLeftArm = true;
    internal bool shouldTrackRightArm = true;
    internal bool shouldTrackLeftLeg = true;
    internal bool shouldTrackRightLeg = true;
    internal bool shouldTrackPelvis = true;
    internal bool shouldTrackLocomotion = true;

    // VRIK Calibration Info
    internal VRIKLocomotionData _locomotionData;

    // Last Movement Parent Info
    internal Vector3 _movementPosition;
    internal Quaternion _movementRotation;
    internal CVRMovementParent _movementParent;

    // Solver Info
    internal float _scaleDifference = 1f;
    internal float _locomotionWeight = 1f;
    internal float _ikSimulatedRootAngle = 0f;
    internal bool _wasTrackingLocomotion = false;

    #endregion

    #region Virtual Game Methods

    public virtual void OnInitializeIk() { }

    public virtual void OnPlayerScaled(float scaleDifference)
    {
        VRIKUtils.ApplyScaleToVRIK
        (
            _vrik,
            _locomotionData,
            _scaleDifference = scaleDifference
        );
    }

    public virtual void OnPlayerHandleMovementParent(CVRMovementParent currentParent, Vector3 platformPivot)
    {
        Vector3 currentPosition = currentParent._referencePoint.position;
        Quaternion currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

        Vector3 deltaPosition = currentPosition - _movementPosition;
        Quaternion deltaRotation = Quaternion.Inverse(_movementRotation) * currentRotation;

        if (_movementParent == currentParent)
        {
            _solver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);
            _ikSimulatedRootAngle = Mathf.Repeat(_ikSimulatedRootAngle + deltaRotation.eulerAngles.y, 360f);
        }

        _movementParent = currentParent;
        _movementPosition = currentPosition;
        _movementRotation = currentRotation;
    }

    #endregion

    #region Virtual IK Weights

    public virtual void UpdateWeights()
    {
        if (!shouldTrackAll)
            return;

        if (shouldTrackHead)
            Update_HeadWeight();

        if (shouldTrackLeftArm)
            Update_LeftArmWeight();

        if (shouldTrackRightArm)
            Update_RightArmWeight();

        if (shouldTrackLeftLeg)
            Update_LeftLegWeight();

        if (shouldTrackRightLeg)
            Update_RightLegWeight();

        if (shouldTrackPelvis)
            Update_PelvisWeight();

        if (shouldTrackLocomotion)
        {
            Update_LocomotionWeight();
            ResetSolverIfNeeded();
        }

        Update_IKPositionWeight();
    }

    protected virtual void Update_HeadWeight()
    {
        float targetWeight = GetTargetWeight(BodyControl.TrackingHead, true);
        BodyControl.SetHeadWeight(_solver.spine, targetWeight);
        BodyControl.SetLookAtWeight(IKManager.lookAtIk, targetWeight);
    }

    protected virtual void Update_LeftArmWeight()
    {
        float leftArmWeight = GetTargetWeight(BodyControl.TrackingLeftArm, _solver.leftArm.target != null);
        BodyControl.SetArmWeight(_solver.leftArm, leftArmWeight);
    }

    protected virtual void Update_RightArmWeight()
    {
        float rightArmWeight = GetTargetWeight(BodyControl.TrackingRightArm, _solver.rightArm.target != null);
        BodyControl.SetArmWeight(_solver.rightArm, rightArmWeight);
    }

    protected virtual void Update_LeftLegWeight()
    {
        float leftLegWeight = GetTargetWeight(BodyControl.TrackingLeftLeg, _solver.leftLeg.target != null);
        BodyControl.SetLegWeight(_solver.leftLeg, leftLegWeight);
    }

    protected virtual void Update_RightLegWeight()
    {
        float rightLegWeight = GetTargetWeight(BodyControl.TrackingRightLeg, _solver.rightLeg.target != null);
        BodyControl.SetLegWeight(_solver.rightLeg, rightLegWeight);
    }

    protected virtual void Update_PelvisWeight()
    {
        float pelvisWeight = GetTargetWeight(BodyControl.TrackingPelvis, _solver.spine.pelvisTarget != null);
        BodyControl.SetPelvisWeight(_solver.spine, pelvisWeight);
    }

    protected virtual void Update_LocomotionWeight()
    {
        _locomotionWeight = Mathf.Lerp(_locomotionWeight, BodyControl.TrackingLocomotion ? 1f : 0f,
            Time.deltaTime * ModSettings.EntryIKLerpSpeed.Value * 2f);
        BodyControl.SetLocomotionWeight(_solver.locomotion, _locomotionWeight);
    }

    protected virtual void Update_IKPositionWeight()
    {
        float ikPositionWeight = BodyControl.TrackingAll ? BodyControl.TrackingIKPositionWeight : 0f;
        BodyControl.SetIKPositionWeight(_solver, ikPositionWeight);
        BodyControl.SetIKPositionWeight(IKManager.lookAtIk, ikPositionWeight);
    }

    protected virtual float GetTargetWeight(bool isTracking, bool hasTarget)
    {
        return isTracking && hasTarget ? 1f : 0f;
    }

    #endregion

    #region Private Methods

    private void ResetSolverIfNeeded()
    {
        if (_wasTrackingLocomotion == BodyControl.TrackingLocomotion)
            return;

        _wasTrackingLocomotion = BodyControl.TrackingLocomotion;
        VRIKUtils.ResetToInitialFootsteps(_vrik, _locomotionData, _scaleDifference);
        _solver.Reset();
    }

    #endregion
}