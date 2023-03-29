using ABI.CCK.Components;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRIK;

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
    public bool Setting_PlantFeet = true;
    public float Setting_BodyLeanWeight;
    public float Setting_BodyHeadingLimit;
    public float Setting_PelvisHeadingWeight;
    public float Setting_ChestHeadingWeight;

    // Calibration Settings
    public bool Setting_UseVRIKToes = true;
    public bool Setting_FindUnmappedToes = true;

    // Integration Settings
    public bool Setting_IntegrationAMT = false;

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
    HumanPose _initialHumanPose;
    HumanPoseHandler _humanPoseHandler;

    // Animator Info
    int _locomotionLayer = -1;
    int _customIKPoseLayer = -1;
    bool _requireFixTransforms = false;

    // VRIK Calibration Info
    Vector3 _leftKneeNormal;
    Vector3 _rightKneeNormal;
    float _initialFootDistance;
    float _initialStepThreshold;
    float _initialStepHeight;

    // Player Info
    Transform _cameraTransform = null;
    bool _isEmotePlaying = false;
    float _simulatedRootAngle = 0f;
    float _locomotionWeight = 1f;
    float _locomotionWeightLerp = 1f;
    float _locomotionLerpSpeed = 10f;

    // Last Movement Parent Info
    Vector3 _previousPosition;
    Quaternion _previousRotation;

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
        LerpLocomotionWeight();
        ApplyBodySystemWeights();
    }

    void HandleLocomotionTracking()
    {
        bool isMoving = movementSystem.movementVector.magnitude > 0f;
        bool isGrounded = movementSystem._isGrounded;
        bool isCrouching = movementSystem.crouching;
        bool isProne = movementSystem.prone;
        bool isFlying = movementSystem.flying;

        // Why do it myself if VRIK already does the maths
        Vector3 headLocalPos = avatarIKSolver.spine.headPosition - avatarIKSolver.spine.rootPosition;
        float upright = 1f + (headLocalPos.y - avatarIKSolver.spine.headHeight);

        if (isMoving || isCrouching || isProne || isFlying || !isGrounded)
        {
            if (BodySystem.TrackingLocomotionEnabled)
            {
                BodySystem.TrackingLocomotionEnabled = false;
                avatarIKSolver.Reset();
                ResetDesktopVRIK();
            }
        }
        else
        {
            if (!BodySystem.TrackingLocomotionEnabled && upright > 0.8f)
            {
                BodySystem.TrackingLocomotionEnabled = true;
                avatarIKSolver.Reset();
                ResetDesktopVRIK();
            }
        }
    }

    void LerpLocomotionWeight()
    {
        _locomotionWeight = BodySystem.TrackingEnabled && BodySystem.TrackingLocomotionEnabled ? 1.0f : 0.0f;
        _locomotionWeightLerp = Mathf.Lerp(_locomotionWeightLerp, _locomotionWeight, Time.deltaTime * _locomotionLerpSpeed);
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

            SetArmWeight(avatarIKSolver.leftArm, false);
            SetArmWeight(avatarIKSolver.rightArm, false);
            SetLegWeight(avatarIKSolver.leftLeg, false);
            SetLegWeight(avatarIKSolver.rightLeg, false);
        }
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
            _initialFootDistance,
            _initialStepThreshold,
            _initialStepHeight,
            scaleDifference
        );

        avatarIKSolver.Reset();
        ResetDesktopVRIK();
        return true;
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        if (avatarVRIK == null) return;

        if (isEmotePlaying == _isEmotePlaying) return;
        
        _isEmotePlaying = isEmotePlaying;

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

        CVRMovementParent currentParent = movementSystem._currentParent;
        if (currentParent == null) return false;

        Transform referencePoint = currentParent._referencePoint;
        if (referencePoint == null) return false;

        var currentPosition = referencePoint.position;
        var currentRotation = currentParent.transform.rotation;

        // Keep only the Y-axis rotation
        currentRotation = Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);

        var deltaPosition = currentPosition - _previousPosition;
        var deltaRotation = Quaternion.Inverse(_previousRotation) * currentRotation;

        var platformPivot = transform.position;
        avatarIKSolver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);

        _previousPosition = currentPosition;
        _previousRotation = currentRotation;

        ResetDesktopVRIK();
        return true;
    }

    public void OnPreSolverUpdate()
    {
        // Reset avatar offset
        avatarTransform.localPosition = Vector3.zero;
        avatarTransform.localRotation = Quaternion.identity;

        if (_isEmotePlaying) return;

        // Set plant feet
        avatarIKSolver.plantFeet = Setting_PlantFeet;

        // Emulate old VRChat hip movementSystem
        if (Setting_BodyLeanWeight > 0)
        {
            float weightedAngle = Setting_BodyLeanWeight * _locomotionWeightLerp;
            float angle = _cameraTransform.localEulerAngles.x;
            angle = angle > 180 ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, avatarTransform.right);
            avatarIKSolver.spine.headRotationOffset *= rotation;
        }

        // Make root heading follow within a set limit
        if (Setting_BodyHeadingLimit > 0)
        {
            float weightedAngleLimit = Setting_BodyHeadingLimit * _locomotionWeightLerp;
            float deltaAngleRoot = Mathf.DeltaAngle(transform.eulerAngles.y, _simulatedRootAngle);
            float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);
            if (absDeltaAngleRoot > weightedAngleLimit)
            {
                deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                _simulatedRootAngle = Mathf.MoveTowardsAngle(_simulatedRootAngle, transform.eulerAngles.y, absDeltaAngleRoot - weightedAngleLimit);
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
    }

    void ResetDesktopVRIK()
    {
        _simulatedRootAngle = transform.eulerAngles.y;
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
        _locomotionLayer = avatarAnimator.GetLayerIndex("IKPose");
        _customIKPoseLayer = avatarAnimator.GetLayerIndex("Locomotion/Emotes");

        // Dispose and create new _humanPoseHandler
        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(avatarAnimator.avatar, avatarTransform);

        // Get initial human poses
        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.GetHumanPose(ref _initialHumanPose);

        // Dumb fix for rare upload issue
        _requireFixTransforms = !avatarAnimator.enabled;

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
        avatarVRIK.fixTransforms = _requireFixTransforms || foundUnmappedToes;

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
        VRIKUtils.CalculateKneeBendNormals(avatarVRIK, out _leftKneeNormal, out _rightKneeNormal);

        SetAvatarPose(AvatarPose.IKPose);

        // Calculate initial IK scaling values with IKPose
        VRIKUtils.CalculateInitialIKScaling(avatarVRIK, out _initialFootDistance, out _initialStepThreshold, out _initialStepHeight);

        // Setup HeadIKTarget
        VRIKUtils.SetupHeadIKTarget(avatarVRIK);

        // Initiate VRIK manually
        VRIKUtils.InitiateVRIKSolver(avatarVRIK);

        SetAvatarPose(AvatarPose.Initial);
    }

    void ConfigureVRIK()
    {
        VRIKUtils.ApplyScaleToVRIK
        (
            avatarVRIK,
            _initialFootDistance,
            _initialStepThreshold,
            _initialStepHeight,
            1f
        );
        VRIKUtils.ApplyKneeBendNormals(avatarVRIK, _leftKneeNormal, _rightKneeNormal);
        avatarVRIK.onPreSolverUpdate.AddListener(new UnityAction(DesktopVRIKSystem.Instance.OnPreSolverUpdate));
    }

    void SetAvatarPose(AvatarPose pose)
    {
        switch (pose)
        {
            case AvatarPose.Default:
                if (HasCustomIKPose())
                {
                    SetCustomLayersWeights(0f, 1f);
                    return;
                }
                SetMusclesToValue(0f);
                break;
            case AvatarPose.Initial:
                _humanPoseHandler.SetHumanPose(ref _initialHumanPose);
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
        return _locomotionLayer != -1 && _customIKPoseLayer != -1;
    }

    void SetCustomLayersWeights(float customIKPoseLayerWeight, float locomotionLayerWeight)
    {
        avatarAnimator.SetLayerWeight(_customIKPoseLayer, customIKPoseLayerWeight);
        avatarAnimator.SetLayerWeight(_locomotionLayer, locomotionLayerWeight);
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
