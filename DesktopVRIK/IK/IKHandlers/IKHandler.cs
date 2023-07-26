using ABI.CCK.Components;
using ABI_RC.Systems.IK.SubSystems;
using NAK.DesktopVRIK.IK.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.DesktopVRIK.IK.IKHandlers;

internal abstract class IKHandler
{
    #region Variables

    internal VRIK _vrik;
    internal IKSolverVR _solver;

    // VRIK Calibration Info
    internal VRIKLocomotionData _locomotionData;

    // Last Movement Parent Info
    internal Vector3 _movementPosition;
    internal Quaternion _movementRotation;
    internal CVRMovementParent _movementParent;

    // Solver Info
    internal float _scaleDifference = 1f;
    internal float _ikWeight = 1f;
    internal float _locomotionWeight = 1f;
    internal float _ikSimulatedRootAngle;
    internal bool _wasTrackingLocomotion;

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
        Update_HeadWeight();

        Update_LeftArmWeight();
        Update_RightArmWeight();

        Update_LeftLegWeight();
        Update_RightLegWeight();

        Update_PelvisWeight();

        Update_LocomotionWeight();
        ResetSolverIfNeeded();

        Update_IKPositionWeight();
    }

    protected virtual void Update_HeadWeight()
    {
        // There is no Head tracking setting
        _solver.spine.rotationWeight = _solver.leftArm.positionWeight =
            GetTargetWeight(BodySystem.TrackingEnabled, _solver.spine.headTarget != null);
    }

    protected virtual void Update_LeftArmWeight()
    {
        _solver.leftArm.rotationWeight = _solver.leftArm.positionWeight =
            GetTargetWeight(BodySystem.TrackingLeftArmEnabled, _solver.leftArm.target != null);
    }

    protected virtual void Update_RightArmWeight()
    {
        _solver.rightArm.rotationWeight = _solver.rightArm.positionWeight =
            GetTargetWeight(BodySystem.TrackingRightArmEnabled, _solver.rightArm.target != null);
    }

    protected virtual void Update_LeftLegWeight()
    {
        _solver.leftLeg.rotationWeight = _solver.leftLeg.positionWeight =
            GetTargetWeight(BodySystem.TrackingLeftLegEnabled, _solver.leftLeg.target != null);
    }

    protected virtual void Update_RightLegWeight()
    {
        _solver.rightLeg.rotationWeight = _solver.rightLeg.positionWeight =
            GetTargetWeight(BodySystem.TrackingRightLegEnabled, _solver.rightLeg.target != null);
    }

    protected virtual void Update_PelvisWeight()
    {
        // There is no Pelvis tracking setting
        _solver.spine.pelvisRotationWeight = _solver.spine.pelvisPositionWeight =
            GetTargetWeight(BodySystem.TrackingEnabled, _solver.spine.pelvisTarget != null);
    }

    protected virtual void Update_LocomotionWeight()
    {
        _solver.locomotion.weight = _locomotionWeight = Mathf.Lerp(_locomotionWeight, BodySystem.TrackingLocomotionEnabled ? 1f : 0f,
            Time.deltaTime * ModSettings.EntryIKLerpSpeed.Value * 2f);
    }

    protected virtual void Update_IKPositionWeight()
    {
        _solver.IKPositionWeight = _ikWeight = Mathf.Lerp(_ikWeight, BodySystem.TrackingEnabled ? BodySystem.TrackingPositionWeight : 0f,
            Time.deltaTime * ModSettings.EntryIKLerpSpeed.Value);
    }
    
    public virtual void Reset()
    {
        _ikSimulatedRootAngle = _vrik.transform.eulerAngles.y;
        if(ModSettings.EntryResetFootstepsOnIdle.Value)
            VRIKUtils.ResetToInitialFootsteps(_vrik, _locomotionData, _scaleDifference);

        _solver.Reset();
    }
    
    #endregion

    #region Private Methods

    private float GetTargetWeight(bool isTracking, bool hasTarget)
    {
        return isTracking && hasTarget ? 1f : 0f;
    }

    private void ResetSolverIfNeeded()
    {
        if (_wasTrackingLocomotion == BodySystem.TrackingLocomotionEnabled)
            return;

        _wasTrackingLocomotion = BodySystem.TrackingLocomotionEnabled;
        if (ModSettings.EntryResetFootstepsOnIdle.Value)
            VRIKUtils.ResetToInitialFootsteps(_vrik, _locomotionData, _scaleDifference);

        _solver.Reset();
    }

    #endregion
}