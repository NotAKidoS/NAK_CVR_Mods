using ABI.CCK.Components;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.DesktopVRIK;

internal class DesktopVRIKSystem : MonoBehaviour
{
    public static DesktopVRIKSystem Instance;
    public static Dictionary<HumanBodyBones, bool> BoneExists;
    public static readonly float[] IKPoseMuscles = new float[]
    {
            0.00133321f,
            8.195831E-06f,
            8.537738E-07f,
            -0.002669832f,
            -7.651234E-06f,
            -0.001659694f,
            0f,
            0f,
            0f,
            0.04213953f,
            0.0003007996f,
            -0.008032114f,
            -0.03059979f,
            -0.0003182998f,
            0.009640567f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0.5768794f,
            0.01061097f,
            -0.1127839f,
            0.9705755f,
            0.07972051f,
            -0.0268422f,
            0.007237188f,
            0f,
            0.5768792f,
            0.01056608f,
            -0.1127519f,
            0.9705756f,
            0.07971933f,
            -0.02682396f,
            0.007229362f,
            0f,
            -5.651802E-06f,
            -3.034899E-07f,
            0.4100508f,
            0.3610304f,
            -0.0838329f,
            0.9262537f,
            0.1353517f,
            -0.03578902f,
            0.06005657f,
            -4.95989E-06f,
            -1.43007E-06f,
            0.4096187f,
            0.363263f,
            -0.08205152f,
            0.9250782f,
            0.1345718f,
            -0.03572125f,
            0.06055461f,
            -1.079177f,
            0.2095419f,
            0.6140652f,
            0.6365265f,
            0.6683931f,
            -0.4764312f,
            0.8099416f,
            0.8099371f,
            0.6658203f,
            -0.7327053f,
            0.8113618f,
            0.8114051f,
            0.6643661f,
            -0.40341f,
            0.8111364f,
            0.8111367f,
            0.6170399f,
            -0.2524227f,
            0.8138723f,
            0.8110135f,
            -1.079171f,
            0.2095456f,
            0.6140658f,
            0.6365255f,
            0.6683878f,
            -0.4764301f,
            0.8099402f,
            0.8099376f,
            0.6658241f,
            -0.7327023f,
            0.8113653f,
            0.8113793f,
            0.664364f,
            -0.4034042f,
            0.811136f,
            0.8111364f,
            0.6170469f,
            -0.2524345f,
            0.8138595f,
            0.8110138f
     };

    enum AvatarPose
    {
        Default = 0,
        Initial = 1,
        IKPose = 2,
        TPose = 3
    }

    // DesktopVRIK Settings
    public bool Setting_Enabled = true;
    public bool Setting_PlantFeet;
    public bool Setting_ResetFootsteps;
    public bool Setting_ProneThrusting;
    public float Setting_BodyLeanWeight;
    public float Setting_BodyHeadingLimit;
    public float Setting_PelvisHeadingWeight;
    public float Setting_ChestHeadingWeight;
    public float Setting_IKLerpSpeed;

    // Calibration Settings
    public bool Setting_UseVRIKToes;
    public bool Setting_FindUnmappedToes;

    // Integration Settings
    public bool Setting_IntegrationAMT;

    // Avatar Components
    public CVRAvatar avatarDescriptor = null;
    public Animator avatarAnimator = null;
    public Transform avatarTransform = null;
    public LookAtIK avatarLookAtIK = null;
    public VRIK avatarVRIK = null;
    public IKSolverVR avatarIKSolver = null;

    // ChilloutVR Player Components
    PlayerSetup playerSetup;
    MovementSystem movementSystem;

    // Calibration Objects
    HumanPose _humanPose;
    HumanPose _humanPoseInitial;
    HumanPoseHandler _humanPoseHandler;

    // Animator Info
    int _animLocomotionLayer = -1;
    int _animIKPoseLayer = -1;

    // VRIK Calibration Info
    Vector3 _vrikKneeNormalLeft;
    Vector3 _vrikKneeNormalRight;
    Vector3 _vrikInitialFootPosLeft;
    Vector3 _vrikInitialFootPosRight;
    Quaternion _vrikInitialFootRotLeft;
    Quaternion _vrikInitialFootRotRight;
    float _vrikInitialFootDistance;
    float _vrikInitialStepThreshold;
    float _vrikInitialStepHeight;
    bool _vrikFixTransformsRequired;

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
    CVRMovementParent _currentParent;

    DesktopVRIKSystem()
    {
        BoneExists = new Dictionary<HumanBodyBones, bool>();
    }

    void Start()
    {
        Instance = this;

        playerSetup = GetComponent<PlayerSetup>();
        movementSystem = GetComponent<MovementSystem>();

        _cameraTransform = playerSetup.desktopCamera.transform;

        DesktopVRIKMod.UpdateAllSettings();
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
            avatarIKSolver.Reset();
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
        bool isStanding = IsStanding();

        return !(isMoving || isCrouching || isProne || isFlying || !isGrounded || !isStanding);
    }

    bool IsStanding()
    {
        // Let AMT handle it if available
        if (Setting_IntegrationAMT) return true;

        // Get Upright value
        Vector3 delta = avatarIKSolver.spine.headPosition - avatarTransform.position;
        Vector3 deltaRotated = Quaternion.Euler(0, avatarTransform.rotation.eulerAngles.y, 0) * delta;
        float upright = Mathf.InverseLerp(0f, avatarIKSolver.spine.headHeight * _scaleDifference, deltaRotated.y);
        return upright > 0.85f;
    }


    void UpdateLocomotionWeight()
    {
        float targetWeight = BodySystem.TrackingEnabled && BodySystem.TrackingLocomotionEnabled ? 1.0f : 0.0f;
        if (Setting_IKLerpSpeed > 0)
        {
            _ikWeightLerp = Mathf.Lerp(_ikWeightLerp, targetWeight, Time.deltaTime * Setting_IKLerpSpeed);
            _locomotionWeight = Mathf.Lerp(_locomotionWeight, targetWeight, Time.deltaTime * Setting_IKLerpSpeed * 2f);
        }
        else
        {
            _ikWeightLerp = targetWeight;
            _locomotionWeight = targetWeight;
        }
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
            avatarIKSolver.IKPositionWeight = BodySystem.TrackingPositionWeight;
            avatarIKSolver.locomotion.weight = _locomotionWeight;

            bool useAnimatedBendNormal = _locomotionWeight <= 0.5f;
            avatarIKSolver.leftLeg.useAnimatedBendNormal = useAnimatedBendNormal;
            avatarIKSolver.rightLeg.useAnimatedBendNormal = useAnimatedBendNormal;
            SetArmWeight(avatarIKSolver.leftArm, BodySystem.TrackingLeftArmEnabled && avatarIKSolver.leftArm.target != null);
            SetArmWeight(avatarIKSolver.rightArm, BodySystem.TrackingRightArmEnabled && avatarIKSolver.rightArm.target != null);
            SetLegWeight(avatarIKSolver.leftLeg, BodySystem.TrackingLeftLegEnabled && avatarIKSolver.leftLeg.target != null);
            SetLegWeight(avatarIKSolver.rightLeg, BodySystem.TrackingRightLegEnabled && avatarIKSolver.rightLeg.target != null);
        }
        else
        {
            avatarVRIK.enabled = false;
            avatarIKSolver.IKPositionWeight = 0f;
            avatarIKSolver.locomotion.weight = 0f;

            avatarIKSolver.leftLeg.useAnimatedBendNormal = false;
            avatarIKSolver.rightLeg.useAnimatedBendNormal = false;
            SetArmWeight(avatarIKSolver.leftArm, false);
            SetArmWeight(avatarIKSolver.rightArm, false);
            SetLegWeight(avatarIKSolver.leftLeg, false);
            SetLegWeight(avatarIKSolver.rightLeg, false);
        }
    }

    void ResetAvatarLocalPosition()
    {
        // Reset avatar offset
        avatarTransform.localPosition = Vector3.zero;
        avatarTransform.localRotation = Quaternion.identity;
    }

    public void OnSetupAvatarDesktop()
    {
        if (!Setting_Enabled) return;

        CalibrateDesktopVRIK();
        ResetDesktopVRIK();
    }

    public bool OnSetupIKScaling(float scaleDifference)
    {
        if (avatarVRIK == null) return false;

        VRIKUtils.ApplyScaleToVRIK
        (
            avatarVRIK,
            _vrikInitialFootDistance,
            _vrikInitialStepThreshold,
            _vrikInitialStepHeight,
            scaleDifference
        );

        _scaleDifference = scaleDifference;

        avatarIKSolver.Reset();
        ResetDesktopVRIK();
        return true;
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        if (avatarVRIK == null) return;

        if (isEmotePlaying == _ikEmotePlaying) return;
        _ikEmotePlaying = isEmotePlaying;

        if (avatarLookAtIK != null)
            avatarLookAtIK.enabled = !isEmotePlaying;

        // Disable tracking completely while emoting
        BodySystem.TrackingEnabled = !isEmotePlaying;
        avatarIKSolver.Reset();
        ResetDesktopVRIK();
    }

    public bool OnPlayerSetupResetIk()
    {
        if (avatarVRIK == null) return false;

        // Check if PlayerSetup.ResetIk() was called for movement parent
        CVRMovementParent currentParent = movementSystem._currentParent;
        if (currentParent == null || currentParent._referencePoint == null) return false;

        // Get current position
        var currentPosition = currentParent._referencePoint.position;
        var currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

        // Convert to delta position (how much changed since last frame)
        var deltaPosition = currentPosition - _movementPosition;
        var deltaRotation = Quaternion.Inverse(_movementRotation) * currentRotation;

        // desktop pivots from playerlocal transform
        var platformPivot = transform.position;

        // Prevent targeting other parent position
        if (_currentParent == currentParent)
        {
            // Add platform motion to IK solver
            avatarIKSolver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);
            ResetDesktopVRIK();
        }

        // Store for next frame
        _currentParent = currentParent;
        _movementPosition = currentPosition;
        _movementRotation = currentRotation;

        return true;
    }

    public void OnPreSolverUpdate()
    {
        // Set plant feet
        avatarIKSolver.plantFeet = Setting_PlantFeet;

        // Apply custom VRIK solving effects
        IKBodyLeaningOffset(_ikWeightLerp);
        IKBodyHeadingOffset(_ikWeightLerp);
    }

    void IKBodyLeaningOffset(float weight)
    {
        // Emulate old VRChat hip movement
        if (Setting_BodyLeanWeight <= 0) return;

        if (Setting_ProneThrusting) weight = 1f;
        float weightedAngle = Setting_BodyLeanWeight * weight;
        float angle = _cameraTransform.localEulerAngles.x;
        angle = angle > 180 ? angle - 360 : angle;
        Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, avatarTransform.right);
        avatarIKSolver.spine.headRotationOffset *= rotation;
    }

    void IKBodyHeadingOffset(float weight)
    {
        // Make root heading follow within a set limit
        if (Setting_BodyHeadingLimit <= 0) return;

        float weightedAngleLimit = Setting_BodyHeadingLimit * weight;
        float deltaAngleRoot = Mathf.DeltaAngle(transform.eulerAngles.y, _ikSimulatedRootAngle);
        float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);

        if (absDeltaAngleRoot > weightedAngleLimit)
        {
            deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
            _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, transform.eulerAngles.y, absDeltaAngleRoot - weightedAngleLimit);
        }

        avatarIKSolver.spine.rootHeadingOffset = deltaAngleRoot;

        if (Setting_PelvisHeadingWeight > 0)
        {
            avatarIKSolver.spine.pelvisRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * Setting_PelvisHeadingWeight, 0f);
            avatarIKSolver.spine.chestRotationOffset *= Quaternion.Euler(0f, -deltaAngleRoot * Setting_PelvisHeadingWeight, 0f);
        }

        if (Setting_ChestHeadingWeight > 0)
        {
            avatarIKSolver.spine.chestRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * Setting_ChestHeadingWeight, 0f);
        }
    }

    void IKResetFootsteps()
    {
        // Reset footsteps immediatly to initial
        if (!Setting_ResetFootsteps) return;

        VRIKUtils.SetFootsteps
        (
            avatarVRIK,
            _vrikInitialFootPosLeft,
            _vrikInitialFootPosRight,
            _vrikInitialFootRotLeft,
            _vrikInitialFootRotRight
        );
    }

    void ResetDesktopVRIK()
    {
        _ikSimulatedRootAngle = transform.eulerAngles.y;
    }

    void CalibrateDesktopVRIK()
    {
        ScanAvatar();
        SetupVRIK();
        CalibrateVRIK();
        ConfigureVRIK();
    }

    void ScanAvatar()
    {
        // Find required avatar components
        avatarDescriptor = playerSetup._avatarDescriptor;
        avatarAnimator = playerSetup._animator;
        avatarTransform = playerSetup._avatar.transform;
        avatarLookAtIK = playerSetup.lookIK;

        // Get animator layer inticies
        _animIKPoseLayer = avatarAnimator.GetLayerIndex("IKPose");
        _animLocomotionLayer = avatarAnimator.GetLayerIndex("Locomotion/Emotes");

        // Dispose and create new _humanPoseHandler
        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(avatarAnimator.avatar, avatarTransform);

        // Get initial human poses
        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.GetHumanPose(ref _humanPoseInitial);

        // Dumb fix for rare upload issue
        _vrikFixTransformsRequired = !avatarAnimator.enabled;

        // Find available HumanoidBodyBones
        BoneExists.Clear();
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone != HumanBodyBones.LastBone)
            {
                BoneExists.Add(bone, avatarAnimator.GetBoneTransform(bone) != null);
            }
        }
    }

    void SetupVRIK()
    {
        // Add and configure VRIK
        avatarVRIK = avatarTransform.AddComponentIfMissing<VRIK>();
        avatarVRIK.AutoDetectReferences();
        avatarIKSolver = avatarVRIK.solver;

        VRIKUtils.ConfigureVRIKReferences(avatarVRIK, Setting_UseVRIKToes, Setting_FindUnmappedToes, out bool foundUnmappedToes);

        // Fix animator issue or non-human mapped toes
        avatarVRIK.fixTransforms = _vrikFixTransformsRequired || foundUnmappedToes;

        // Default solver settings
        avatarIKSolver.locomotion.weight = 0f;
        avatarIKSolver.locomotion.angleThreshold = 30f;
        avatarIKSolver.locomotion.maxLegStretch = 1f;
        avatarIKSolver.spine.minHeadHeight = 0f;
        avatarIKSolver.IKPositionWeight = 1f;
        avatarIKSolver.spine.chestClampWeight = 0f;
        avatarIKSolver.spine.maintainPelvisPosition = 0f;

        // Body leaning settings
        avatarIKSolver.spine.neckStiffness = 0.0001f;
        avatarIKSolver.spine.bodyPosStiffness = 1f;
        avatarIKSolver.spine.bodyRotStiffness = 0.2f;

        // Disable locomotion
        avatarIKSolver.locomotion.velocityFactor = 0f;
        avatarIKSolver.locomotion.maxVelocity = 0f;
        avatarIKSolver.locomotion.rootSpeed = 1000f;

        // Disable chest rotation by hands
        avatarIKSolver.spine.rotateChestByHands = 0f;

        // Prioritize LookAtIK
        avatarIKSolver.spine.headClampWeight = 0.2f;

        // Disable going on tippytoes
        avatarIKSolver.spine.positionWeight = 0f;
        avatarIKSolver.spine.rotationWeight = 1f;

        // Set so emotes play properly
        avatarIKSolver.spine.maxRootAngle = 180f;

        // We disable these ourselves now, as we no longer use BodySystem
        avatarIKSolver.spine.maintainPelvisPosition = 1f;
        avatarIKSolver.spine.positionWeight = 0f;
        avatarIKSolver.spine.pelvisPositionWeight = 0f;
        avatarIKSolver.leftArm.positionWeight = 0f;
        avatarIKSolver.leftArm.rotationWeight = 0f;
        avatarIKSolver.rightArm.positionWeight = 0f;
        avatarIKSolver.rightArm.rotationWeight = 0f;
        avatarIKSolver.leftLeg.positionWeight = 0f;
        avatarIKSolver.leftLeg.rotationWeight = 0f;
        avatarIKSolver.rightLeg.positionWeight = 0f;
        avatarIKSolver.rightLeg.rotationWeight = 0f;

        // This is now our master Locomotion weight
        avatarIKSolver.locomotion.weight = 1f;
        avatarIKSolver.IKPositionWeight = 1f;
    }

    void CalibrateVRIK()
    {
        SetAvatarPose(AvatarPose.Default);

        // Calculate bend normals with motorcycle pose
        VRIKUtils.CalculateKneeBendNormals(avatarVRIK, out _vrikKneeNormalLeft, out _vrikKneeNormalRight);

        SetAvatarPose(AvatarPose.IKPose);

        // Calculate initial IK scaling values with IKPose
        VRIKUtils.CalculateInitialIKScaling(avatarVRIK, out _vrikInitialFootDistance, out _vrikInitialStepThreshold, out _vrikInitialStepHeight);

        // Calculate initial Footstep positions
        VRIKUtils.CalculateInitialFootsteps(avatarVRIK, out _vrikInitialFootPosLeft, out _vrikInitialFootPosRight, out _vrikInitialFootRotLeft, out _vrikInitialFootRotRight);

        // Setup HeadIKTarget
        VRIKUtils.SetupHeadIKTarget(avatarVRIK);

        // Initiate VRIK manually
        VRIKUtils.InitiateVRIKSolver(avatarVRIK);

        SetAvatarPose(AvatarPose.Initial);
    }

    void ConfigureVRIK()
    {
        // Reset scale diffrence
        _scaleDifference = 1f;
        VRIKUtils.ApplyScaleToVRIK
        (
            avatarVRIK,
            _vrikInitialFootDistance,
            _vrikInitialStepThreshold,
            _vrikInitialStepHeight,
            1f
        );
        VRIKUtils.ApplyKneeBendNormals(avatarVRIK, _vrikKneeNormalLeft, _vrikKneeNormalRight);
        avatarVRIK.onPreSolverUpdate.AddListener(new UnityAction(DesktopVRIKSystem.Instance.OnPreSolverUpdate));
    }

    void SetAvatarPose(AvatarPose pose)
    {
        switch (pose)
        {
            case AvatarPose.Default:
                SetMusclesToValue(0f);
                break;
            case AvatarPose.Initial:
                if (HasCustomIKPose())
                {
                    SetCustomLayersWeights(0f, 1f);
                    return;
                }
                _humanPoseHandler.SetHumanPose(ref _humanPoseInitial);
                break;
            case AvatarPose.IKPose:
                if (HasCustomIKPose())
                {
                    SetCustomLayersWeights(1f, 0f);
                    return;
                }
                SetMusclesToPose(IKPoseMuscles);
                break;
            case AvatarPose.TPose:
                SetMusclesToPose(BodySystem.TPoseMuscles);
                break;
            default:
                break;
        }
    }

    bool HasCustomIKPose()
    {
        return _animLocomotionLayer != -1 && _animIKPoseLayer != -1;
    }

    void SetCustomLayersWeights(float customIKPoseLayerWeight, float locomotionLayerWeight)
    {
        avatarAnimator.SetLayerWeight(_animIKPoseLayer, customIKPoseLayerWeight);
        avatarAnimator.SetLayerWeight(_animLocomotionLayer, locomotionLayerWeight);
        avatarAnimator.Update(0f);
    }

    void SetMusclesToValue(float value)
    {
        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (int i = 0; i < _humanPose.muscles.Length; i++)
        {
            ApplyMuscleValue((MuscleIndex)i, value, ref _humanPose.muscles);
        }

        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    void SetMusclesToPose(float[] muscles)
    {
        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (int i = 0; i < _humanPose.muscles.Length; i++)
        {
            ApplyMuscleValue((MuscleIndex)i, muscles[i], ref _humanPose.muscles);
        }

        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    void ApplyMuscleValue(MuscleIndex index, float value, ref float[] muscles)
    {
        if (BoneExists.ContainsKey(IKSystem.MusclesToHumanBodyBones[(int)index]) && BoneExists[IKSystem.MusclesToHumanBodyBones[(int)index]])
        {
            muscles[(int)index] = value;
        }
    }
}