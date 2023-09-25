using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using NAK.DesktopVRIK.IK.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.DesktopVRIK.IK.IKHandlers;

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
        _vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdateDesktop);
    }

    #endregion

    #region Weight Overrides

    public override void UpdateWeights()
    {
        // Reset avatar local position
        _vrik.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        base.UpdateWeights();
    }

    public override void UpdateTracking()
    {
        BodySystem.TrackingEnabled = ShouldTrackAll();
        BodySystem.TrackingLocomotionEnabled = BodySystem.TrackingEnabled && ShouldTrackLocomotion();
        ResetSolverIfNeeded();
    }

    #endregion

    #region VRIK Solver Events

    private void OnPreSolverUpdateDesktop()
    {
        // Reset avatar local position
        _vrik.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        _solver.plantFeet = ModSettings.EntryPlantFeet.Value;

        // Emulate old VRChat hip movement
        if (ModSettings.EntryBodyLeanWeight.Value > 0)
        {
            float weightedAngle =  ModSettings.EntryBodyLeanWeight.Value * (ModSettings.EntryProneThrusting.Value ? 1f: _solver.locomotion.weight);
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
    
    private bool ShouldTrackAll()
    {
        return !PlayerSetup.Instance._emotePlaying;
    }

    private bool ShouldTrackLocomotion()
    {
        bool isMoving = MovementSystem.Instance.movementVector.magnitude > 0f;
        bool isGrounded = MovementSystem.Instance._isGrounded;
        bool isCrouching = MovementSystem.Instance.crouching;
        bool isProne = MovementSystem.Instance.prone;
        bool isFlying = MovementSystem.Instance.flying;
        bool isSitting = MovementSystem.Instance.sitting;
        bool isStanding = PlayerSetup.Instance.avatarUpright >=
                          Mathf.Max(PlayerSetup.Instance.avatarProneLimit, PlayerSetup.Instance.avatarCrouchLimit);
        
        return !(isMoving || isCrouching || isProne || isFlying || isSitting || !isGrounded || !isStanding);
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