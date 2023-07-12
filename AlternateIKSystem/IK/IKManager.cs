using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using NAK.AlternateIKSystem.IK.IKHandlers;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.AlternateIKSystem.IK;

public class IKManager : MonoBehaviour
{
    public static IKManager Instance;

    public BodyControl BodyControl = new BodyControl();

    public static VRIK vrik => _vrik;
    private static VRIK _vrik;
    public static IKSolverVR solver => _vrik?.solver;
    private static LookAtIK _lookAtIk;
    public static LookAtIK lookAtIk => _lookAtIk;

    private bool _isAvatarInitialized;

    // IK Handling
    private IKHandler _ikHandler;

    // Player Info
    internal Transform _desktopCamera;
    internal Transform _vrCamera;

    // Avatar Info
    private Animator _animator;
    private Transform _hipTransform;

    // Animator Info
    private int _animLocomotionLayer = -1;
    private int _animIKPoseLayer = -1;

    // Pose Info
    private HumanPoseHandler _humanPoseHandler;
    private HumanPose _humanPose;
    private HumanPose _humanPoseInitial;

    // VRIK Calibration Info
    private VRIKCalibrationData _calibrationData;

    private void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        _desktopCamera = PlayerSetup.Instance.desktopCamera.transform;
        _vrCamera = PlayerSetup.Instance.vrCamera.transform;
    }

    private void Update()
    {
        BodyControl.Update();

        if (!_isAvatarInitialized)
            return;

        _ikHandler?.OnUpdate();
    }

    #region Avatar Events

    public void OnAvatarInitialized(GameObject inAvatar)
    {
        if (_isAvatarInitialized)
            return;

        if (!inAvatar.TryGetComponent(out _animator))
            return;

        if (_animator.avatar == null || !_animator.avatar.isHuman)
            return;

        _lookAtIk = inAvatar.GetComponent<LookAtIK>();

        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        _animIKPoseLayer = _animator.GetLayerIndex("IKPose");
        _animLocomotionLayer = _animator.GetLayerIndex("Locomotion/Emotes");

        _hipTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);

        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);

        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.GetHumanPose(ref _humanPoseInitial);

        if (MetaPort.Instance.isUsingVr)
        {
            InitializeHalfBodyIk();
        }
        else
        {
            InitializeDesktopIk();
        }

        _isAvatarInitialized = true;
    }

    public void OnAvatarDestroyed()
    {
        if (!_isAvatarInitialized)
            return;

        _vrik = null;
        _lookAtIk = null;
        _animator = null;
        _animIKPoseLayer = -1;
        _animLocomotionLayer = -1;
        _hipTransform = null;
        _humanPoseHandler?.Dispose();
        _humanPoseHandler = null;
        _ikHandler = null;

        _isAvatarInitialized = false;
    }

    #endregion

    #region Game Events

    public bool OnPlayerScaled(float scaleDifference)
    {
        if (!_isAvatarInitialized)
            return false;

        _ikHandler?.OnPlayerScaled(scaleDifference, _calibrationData);
        return true;
    }

    public void OnPlayerSeatedStateChanged(bool isSitting)
    {
        if (!_isAvatarInitialized)
            return;

        // update immediately, ShouldTrackLocomotion() will catch up next frame
        if (isSitting)
            BodyControl.TrackingLocomotion = false;
    }

    public bool OnPlayerHandleMovementParent(CVRMovementParent movementParent)
    {
        if (!_isAvatarInitialized)
            return false;

        _ikHandler?.OnPlayerHandleMovementParent(movementParent);
        return true;
    }

    public bool OnPlayerTeleported()
    {
        if (!_isAvatarInitialized)
            return false;
        
        _vrik?.solver.Reset();
        return true;
    }

    #endregion

    #region IK Initialization

    private void InitializeDesktopIk()
    {
        PreSetupIkGeneral();
        IKCalibrator.ConfigureDesktopVrIk(_vrik);
        IKCalibrator.SetupHeadIKTargetDesktop(_vrik);
        InitializeIkGeneral();

        _ikHandler = new IKHandlerDesktop(_vrik);
        _ikHandler.OnInitializeIk();
    }

    private void InitializeHalfBodyIk()
    {
        PreSetupIkGeneral();
        IKCalibrator.ConfigureHalfBodyVrIk(_vrik);
        InitializeIkGeneral();
    }

    private void PreSetupIkGeneral()
    {
        SetAvatarPose(AvatarPose.Default);
        _vrik = IKCalibrator.SetupVrIk(_animator);
    }

    private void InitializeIkGeneral()
    {
        SetAvatarPose(AvatarPose.IKPose);

        VRIKUtils.CalculateInitialIKScaling(_vrik, ref _calibrationData);
        VRIKUtils.CalculateInitialFootsteps(_vrik, ref _calibrationData);

        VRIKUtils.ApplyScaleToVRIK(_vrik, _calibrationData, 1f);

        VRIKUtils.InitiateVRIKSolver(_vrik); // initiate again to store ikpose

        _vrik.onPreSolverUpdate.AddListener(new UnityAction(OnPreSolverUpdateGeneral));
        _vrik.onPostSolverUpdate.AddListener(new UnityAction(OnPostSolverUpdateGeneral));
    }

    #endregion

    #region VRIK Solver Events General

    private void OnPreSolverUpdateGeneral()
    {
        if (solver.IKPositionWeight < 0.9f)
            return;

        Vector3 hipPos = _hipTransform.position;
        Quaternion hipRot = _hipTransform.rotation;

        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (var i = 0; i < _humanPose.muscles.Length; i++)
        {
            //if (IkTweaksSettings.IgnoreAnimationsModeParsed == IgnoreAnimationsMode.All && IKTweaksMod.ourRandomPuck.activeInHierarchy)
            //{
            //    muscles[i] *= ourBoneResetMasks[i] == BoneResetMask.Never ? 1 : 0;
            //    continue;
            //}
            switch (ourBoneResetMasks[i])
            {
                case BoneResetMask.Never:
                    break;
                case BoneResetMask.Spine:
                    _humanPose.muscles[i] *= 1 - solver.spine.pelvisPositionWeight;
                    break;
                case BoneResetMask.LeftArm:
                    _humanPose.muscles[i] *= 1 - solver.leftArm.positionWeight;
                    break;
                case BoneResetMask.RightArm:
                    _humanPose.muscles[i] *= 1 - solver.rightArm.positionWeight;
                    break;
                case BoneResetMask.LeftLeg:
                    _humanPose.muscles[i] *= 1 - solver.leftLeg.positionWeight;
                    break;
                case BoneResetMask.RightLeg:
                    _humanPose.muscles[i] *= 1 - solver.rightLeg.positionWeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _humanPoseHandler.SetHumanPose(ref _humanPose);

        _hipTransform.position = hipPos;
        _hipTransform.rotation = hipRot;
    }

    private void OnPostSolverUpdateGeneral()
    {
        Vector3 hipPos = _hipTransform.position;
        Quaternion hipRot = _hipTransform.rotation;

        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.SetHumanPose(ref _humanPose);

        _hipTransform.position = hipPos;
        _hipTransform.rotation = hipRot;
    }

    #endregion

    #region Avatar Pose Utilities

    private enum AvatarPose
    {
        Default = 0,
        Initial = 1,
        IKPose = 2,
        TPose = 3,
        APose = 4
    }

    private void SetAvatarPose(AvatarPose pose)
    {
        switch (pose)
        {
            case AvatarPose.Default:
                SetMusclesToValue(0f);
                break;
            case AvatarPose.Initial:
                if (HasCustomIKPose())
                    SetCustomLayersWeights(0f, 1f);
                _humanPoseHandler.SetHumanPose(ref _humanPoseInitial);
                break;
            case AvatarPose.IKPose:
                if (HasCustomIKPose())
                {
                    SetCustomLayersWeights(1f, 0f);
                    return;
                }
                SetMusclesToPose(MusclePoses.IKPoseMuscles);
                break;
            case AvatarPose.TPose:
                SetMusclesToPose(MusclePoses.TPoseMuscles);
                break;
            case AvatarPose.APose:
                SetMusclesToPose(MusclePoses.APoseMuscles);
                break;
        }
    }

    private bool HasCustomIKPose()
    {
        return _animLocomotionLayer != -1 && _animIKPoseLayer != -1;
    }

    private void SetCustomLayersWeights(float customIKPoseLayerWeight, float locomotionLayerWeight)
    {
        _animator.SetLayerWeight(_animIKPoseLayer, customIKPoseLayerWeight);
        _animator.SetLayerWeight(_animLocomotionLayer, locomotionLayerWeight);
        _animator.Update(0f);
    }

    private void SetMusclesToValue(float value)
    {
        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (var i = 0; i < ourBoneResetMasks.Length; i++)
        {
            if (ourBoneResetMasks[i] != BoneResetMask.Never)
                _humanPose.muscles[i] = value;
        }

        _humanPose.bodyPosition = Vector3.up;
        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    private void SetMusclesToPose(float[] muscles)
    {
        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (var i = 0; i < ourBoneResetMasks.Length; i++)
        {
            if (ourBoneResetMasks[i] != BoneResetMask.Never)
                _humanPose.muscles[i] = muscles[i];
        }

        _humanPose.bodyPosition = Vector3.up;
        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    #endregion

    #region BodyHandling

    public enum BoneResetMask
    {
        Never,
        Spine,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
    }

    private static readonly string[] ourNeverBones = { "Index", "Thumb", "Middle", "Ring", "Little", "Jaw", "Eye" };
    private static readonly string[] ourArmBones = { "Arm", "Forearm", "Hand", "Shoulder" };
    private static readonly string[] ourLegBones = { "Leg", "Foot", "Toes" };

    private static BoneResetMask JudgeBone(string name)
    {
        if (ourNeverBones.Any(name.Contains))
            return BoneResetMask.Never;

        if (ourArmBones.Any(name.Contains))
        {
            return name.Contains("Left") ? BoneResetMask.LeftArm : BoneResetMask.RightArm;
        }

        if (ourLegBones.Any(name.Contains))
            return name.Contains("Left") ? BoneResetMask.LeftLeg : BoneResetMask.RightLeg;

        return BoneResetMask.Spine;
    }

    internal static readonly BoneResetMask[] ourBoneResetMasks = HumanTrait.MuscleName.Select(JudgeBone).ToArray();

    #endregion
}