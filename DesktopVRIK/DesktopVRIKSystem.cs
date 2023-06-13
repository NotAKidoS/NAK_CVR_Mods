using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using NAK.DesktopVRIK.VRIKHelper;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.DesktopVRIK;

internal class DesktopVRIKSystem : MonoBehaviour
{
    public static DesktopVRIKSystem Instance;
    public static DesktopVRIKCalibrator Calibrator;

    // VRIK Calibration Info
    public VRIKCalibrationData calibrationData;

    // Avatar Components
    public VRIK avatarVRIK = null;
    public LookAtIK avatarLookAtIK = null;
    public Transform avatarTransform = null;
    public CachedSolver cachedSolver;

    // ChilloutVR Player Components
    PlayerSetup playerSetup;
    MovementSystem movementSystem;

    // Player Info
    Transform _cameraTransform;
    bool _ikEmotePlaying;
    float _ikWeightLerp = 1f;
    float _ikSimulatedRootAngle = 0f;
    float _locomotionWeight = 1f;
    float _scaleDifference = 1f;

    // Last Movement Parent Info
    Vector3 _movementPosition;
    Quaternion _movementRotation;
    CVRMovementParent _movementParent;

    void Start()
    {
        Instance = this;
        Calibrator = new DesktopVRIKCalibrator();

        playerSetup = GetComponent<PlayerSetup>();
        movementSystem = GetComponent<MovementSystem>();

        _cameraTransform = playerSetup.desktopCamera.transform;
    }

    void Update()
    {
        if (avatarVRIK == null) return;

        HandleLocomotionTracking();
        UpdateLocomotionWeight();
        ApplyBodySystemWeights();
        ResetAvatarLocalPosition();
    }

    void HandleLocomotionTracking()
    {
        bool shouldTrackLocomotion = ShouldTrackLocomotion();

        if (shouldTrackLocomotion != BodySystem.TrackingLocomotionEnabled)
        {
            BodySystem.TrackingLocomotionEnabled = shouldTrackLocomotion;
            IKResetSolver();
            ResetDesktopVRIK();
            if (shouldTrackLocomotion) IKResetFootsteps();
        }
    }

    bool ShouldTrackLocomotion()
    {
        bool isMoving = movementSystem.movementVector.magnitude > 0f;
        bool isGrounded = movementSystem._isGrounded;
        bool isCrouching = movementSystem.crouching;
        bool isProne = movementSystem.prone;
        bool isFlying = movementSystem.flying;
        bool isSitting = movementSystem.sitting;
        bool isStanding = IsStanding();

        return !(isMoving || isCrouching || isProne || isFlying || isSitting || !isGrounded || !isStanding);
    }

    bool IsStanding()
    {
        // Let AMT handle it if available
        if (DesktopVRIK.EntryIntegrationAMT.Value) return true;

        // Convert head position to avatar's local coordinate system
        Vector3 headPositionInAvatarLocal = avatarTransform.InverseTransformPoint(cachedSolver.Spine.headPosition);

        // Get Upright value (assuming avatar's local up is its standing direction)
        float distance = headPositionInAvatarLocal.y;
        float upright = Mathf.InverseLerp(0f, calibrationData.InitialHeadHeight * _scaleDifference, Mathf.Abs(distance));

        return upright > 0.85f;
    }

    void UpdateLocomotionWeight()
    {
        float targetWeight = BodySystem.TrackingEnabled && BodySystem.TrackingLocomotionEnabled ? 1.0f : 0.0f;
        if (DesktopVRIK.EntryIKLerpSpeed.Value > 0)
        {
            _ikWeightLerp = Mathf.Lerp(_ikWeightLerp, targetWeight, Time.deltaTime * DesktopVRIK.EntryIKLerpSpeed.Value);
            _locomotionWeight = Mathf.Lerp(_locomotionWeight, targetWeight, Time.deltaTime * DesktopVRIK.EntryIKLerpSpeed.Value * 2f);
            return;
        }
        _ikWeightLerp = targetWeight;
        _locomotionWeight = targetWeight;
    }

    void ApplyBodySystemWeights()
    {
        void SetArmWeight(IKSolverVR.Arm arm, bool isTracked)
        {
            arm.positionWeight = isTracked ? 1f : 0f;
            arm.rotationWeight = isTracked ? 1f : 0f;
            arm.shoulderRotationWeight = isTracked ? 1f : 0f;
            arm.shoulderTwistWeight = isTracked ? 1f : 0f;
        }
        void SetLegWeight(IKSolverVR.Leg leg, bool isTracked)
        {
            leg.positionWeight = isTracked ? 1f : 0f;
            leg.rotationWeight = isTracked ? 1f : 0f;
        }

        if (BodySystem.TrackingEnabled)
        {
            avatarVRIK.enabled = true;
            cachedSolver.Solver.IKPositionWeight = BodySystem.TrackingPositionWeight;
            cachedSolver.Locomotion.weight = _locomotionWeight;

            bool useAnimatedBendNormal = _locomotionWeight <= 0.5f;
            cachedSolver.LeftLeg.useAnimatedBendNormal = useAnimatedBendNormal;
            cachedSolver.RightLeg.useAnimatedBendNormal = useAnimatedBendNormal;
            SetArmWeight(cachedSolver.LeftArm, BodySystem.TrackingLeftArmEnabled && cachedSolver.LeftArm.target != null);
            SetArmWeight(cachedSolver.RightArm, BodySystem.TrackingRightArmEnabled && cachedSolver.RightArm.target != null);
            SetLegWeight(cachedSolver.LeftLeg, BodySystem.TrackingLeftLegEnabled && cachedSolver.LeftLeg.target != null);
            SetLegWeight(cachedSolver.RightLeg, BodySystem.TrackingRightLegEnabled && cachedSolver.RightLeg.target != null);
        }
        else
        {
            avatarVRIK.enabled = false;
            cachedSolver.Solver.IKPositionWeight = 0f;
            cachedSolver.Locomotion.weight = 0f;

            cachedSolver.LeftLeg.useAnimatedBendNormal = false;
            cachedSolver.RightLeg.useAnimatedBendNormal = false;
            SetArmWeight(cachedSolver.LeftArm, false);
            SetArmWeight(cachedSolver.RightArm, false);
            SetLegWeight(cachedSolver.LeftLeg, false);
            SetLegWeight(cachedSolver.RightLeg, false);
        }
    }

    void ResetBodySystem()
    {
        // DesktopVRSwitch should handle this, but I am not pushing an update yet.
        BodySystem.TrackingEnabled = true;
        BodySystem.TrackingPositionWeight = 1f;
        BodySystem.isCalibratedAsFullBody = false;
        BodySystem.isCalibrating = false;
        BodySystem.isRecalibration = false;
    }

    void ResetAvatarLocalPosition()
    {
        // Reset avatar offset
        avatarTransform.localPosition = Vector3.zero;
        avatarTransform.localRotation = Quaternion.identity;
    }

    public void OnSetupAvatarDesktop(Animator animator)
    {
        if (!DesktopVRIK.EntryEnabled.Value) return;

        // only run for humanoid avatars
        if (animator != null && animator.avatar != null && animator.avatar.isHuman)
        {
            Calibrator.CalibrateDesktopVRIK(animator);
            ResetBodySystem();
            ResetDesktopVRIK();
        }
    }

    public void OnSetupIKScaling(float scaleDifference)
    {
        _scaleDifference = scaleDifference;

        VRIKUtils.ApplyScaleToVRIK
        (
            avatarVRIK,
            calibrationData,
            _scaleDifference
        );
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        if (isEmotePlaying == _ikEmotePlaying) return;
        _ikEmotePlaying = isEmotePlaying;

        if (avatarLookAtIK != null)
            avatarLookAtIK.enabled = !isEmotePlaying;

        // Disable tracking completely while emoting
        BodySystem.TrackingEnabled = !isEmotePlaying;
        IKResetSolver();
        ResetDesktopVRIK();
    }

    public void OnPlayerSetupSetSitting()
    {
        IKResetSolver();
        ResetDesktopVRIK();
    }

    public void OnPlayerSetupResetIk()
    {
        // Check if PlayerSetup.ResetIk() was called for movement parent
        CVRMovementParent currentParent = movementSystem._currentParent;
        if (currentParent != null && currentParent._referencePoint != null)
        {
            // Get current position
            var currentPosition = currentParent._referencePoint.position;
            var currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

            // Convert to delta position (how much changed since last frame)
            var deltaPosition = currentPosition - _movementPosition;
            var deltaRotation = Quaternion.Inverse(_movementRotation) * currentRotation;

            // desktop pivots from playerlocal transform
            var platformPivot = transform.position;

            // Prevent targeting other parent position
            if (_movementParent == currentParent)
            {
                // Add platform motion to IK solver
                cachedSolver.Solver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);
                ResetDesktopVRIK();
            }

            // Store for next frame
            _movementParent = currentParent;
            _movementPosition = currentPosition;
            _movementRotation = currentRotation;
            return;
        }

        // if not for movementparent, reset ik
        IKResetSolver();
        IKResetFootsteps();
        ResetDesktopVRIK();
    }

    public void OnPreSolverUpdate()
    {
        // Set plant feet
        cachedSolver.Solver.plantFeet = DesktopVRIK.EntryPlantFeet.Value;

        // Apply custom VRIK solving effects
        IKBodyLeaningOffset(_ikWeightLerp);
        IKBodyHeadingOffset(_ikWeightLerp);
    }


    void IKBodyLeaningOffset(float weight)
    {
        // Emulate old VRChat hip movement
        if (DesktopVRIK.EntryBodyLeanWeight.Value <= 0) return;

        if (DesktopVRIK.EntryProneThrusting.Value) weight = 1f;
        float weightedAngle = DesktopVRIK.EntryBodyLeanWeight.Value * weight;
        float angle = _cameraTransform.localEulerAngles.x;
        angle = angle > 180 ? angle - 360 : angle;
        Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, avatarTransform.right);
        cachedSolver.Spine.headRotationOffset *= rotation;
    }

    void IKBodyHeadingOffset(float weight)
    {
        // Make root heading follow within a set limit
        if (DesktopVRIK.EntryBodyHeadingLimit.Value <= 0)
        {
            // reset when value is 0
            cachedSolver.Spine.rootHeadingOffset = 0f;
            return;
        }

        // Get the real localYRotation
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, transform.up);
        Vector3 worldProjectedForward = Vector3.ProjectOnPlane(Vector3.forward, transform.up);

        float localYRotation = Vector3.SignedAngle(projectedForward, worldProjectedForward, transform.up);
        if (localYRotation < 0) localYRotation += 360;

        float weightedAngleLimit = DesktopVRIK.EntryBodyHeadingLimit.Value * weight;
        float deltaAngleRoot = Mathf.DeltaAngle(_ikSimulatedRootAngle, localYRotation);

        float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);
        if (absDeltaAngleRoot > weightedAngleLimit)
        {
            deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
            _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, localYRotation, absDeltaAngleRoot - weightedAngleLimit);
        }
        
        cachedSolver.Spine.rootHeadingOffset = deltaAngleRoot;

        float pelvisHeadingWeight = DesktopVRIK.EntryPelvisHeadingWeight.Value;
        if (pelvisHeadingWeight > 0)
        {
            AdjustBodyPartRotation(deltaAngleRoot * pelvisHeadingWeight, ref cachedSolver.Spine.pelvisRotationOffset);
            AdjustBodyPartRotation(-deltaAngleRoot * pelvisHeadingWeight, ref cachedSolver.Spine.chestRotationOffset);
        }

        float chestHeadingWeight = DesktopVRIK.EntryChestHeadingWeight.Value;
        if (chestHeadingWeight > 0)
        {
            AdjustBodyPartRotation(deltaAngleRoot * chestHeadingWeight, ref cachedSolver.Spine.chestRotationOffset);
        }
    }
    
    void AdjustBodyPartRotation(float angle, ref Quaternion bodyPartRotationOffset)
    {
        // this has to be flipped back cause vrik dumb
        Vector3 localOffset = bodyPartRotationOffset * transform.InverseTransformDirection(new Vector3(0f, angle, 0f));
        bodyPartRotationOffset *= Quaternion.Euler(localOffset);
    }

    public void OnPostSolverUpdate()
    {
        if (!DesktopVRIK.EntryNetIKPass.Value) return;
        Calibrator.ApplyNetIKPass(); // lazy cause Calibrator has humanposehandler
    }

    void IKResetSolver()
    {
        cachedSolver.Solver.Reset();
    }

    void IKResetFootsteps()
    {
        // Reset footsteps immediatly to initial
        if (!DesktopVRIK.EntryResetFootstepsOnIdle.Value) return;

        VRIKUtils.ResetToInitialFootsteps
        (
            avatarVRIK,
            calibrationData,
            _scaleDifference
        );
    }

    void ResetDesktopVRIK()
    {
        _ikSimulatedRootAngle = transform.eulerAngles.y;
    }
}