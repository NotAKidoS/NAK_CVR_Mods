using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK.SubSystems;
using NAK.DesktopVRIK.IK.IKHandlers;
using NAK.DesktopVRIK.IK.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.DesktopVRIK.IK;

public class IKManager : MonoBehaviour
{
    public static IKManager Instance;

    #region Variables

    private static VRIK _vrik;
    private static IKSolverVR _solver;

    private bool _isAvatarInitialized;

    // IK Handling
    private IKHandler _ikHandler;

    // Player Info
    internal Transform _desktopCamera;
    internal Transform _vrCamera;
    private bool _isEmotePlaying;

    // Avatar Info
    private Animator _animator;
    private Transform _hipTransform;

    // Animator Info
    private int _animLocomotionLayer = -1;
    private int _animIKPoseLayer = -1;
    private const string _locomotionLayerName = "Locomotion/Emotes";
    private const string _ikposeLayerName = "IKPose";

    // Pose Info
    private HumanPoseHandler _humanPoseHandler;
    private HumanPose _humanPose;
    private HumanPose _humanPoseInitial;

    #endregion

    #region Unity Methods

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
        if (_ikHandler == null)
            return;

        _ikHandler.UpdateWeights();
    }

    #endregion

    #region Avatar Events

    public void OnAvatarInitialized(GameObject inAvatar)
    {
        if (MetaPort.Instance.isUsingVr)
            return;

        if (_isAvatarInitialized)
            return;

        if (!inAvatar.TryGetComponent(out _animator))
            return;

        if (_animator.avatar == null || !_animator.avatar.isHuman)
            return;

        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        _animIKPoseLayer = _animator.GetLayerIndex(_ikposeLayerName);
        _animLocomotionLayer = _animator.GetLayerIndex(_locomotionLayerName);

        _hipTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);

        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);

        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.GetHumanPose(ref _humanPoseInitial);

        InitializeDesktopIk();

        _isAvatarInitialized = true;
    }

    public void OnAvatarDestroyed()
    {
        if (!_isAvatarInitialized)
            return;

        _vrik = null;
        _solver = null;
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
        if (_ikHandler == null)
            return false;

        _ikHandler.OnPlayerScaled(scaleDifference);
        return true;
    }

    public void OnPlayerSeatedStateChanged(bool isSitting)
    {
        if (_ikHandler == null)
            return;

        _ikHandler.Reset();
    }

    public bool OnPlayerHandleMovementParent(CVRMovementParent movementParent)
    {
        if (_ikHandler == null)
            return false;

        _ikHandler.OnPlayerHandleMovementParent(movementParent, GetPlayerPosition());
        return true;
    }

    public bool OnPlayerTeleported()
    {
        if (_ikHandler == null)
            return false;

        _ikHandler.Reset();
        return true;
    }

    public void OnPlayerUpdate()
    {
        if (_ikHandler == null)
            return;

        _ikHandler.UpdateTracking();
    }

    #endregion

    #region IK Initialization

    private void InitializeDesktopIk()
    {
        SetupIkGeneral();

        IKCalibrator.ConfigureDesktopVrIk(_vrik);
        _ikHandler = new IKHandlerDesktop(_vrik);

        IKCalibrator.SetupHeadIKTarget(_vrik);

        InitializeIkGeneral();

        _ikHandler.OnInitializeIk();
    }

    private void SetupIkGeneral()
    {
        _animator.transform.position = GetPlayerPosition();
        _animator.transform.rotation = GetPlayerRotation();
        SetAvatarPose(AvatarPose.Default);
        _vrik = IKCalibrator.SetupVrIk(_animator);
        _solver = _vrik.solver;
    }

    private void InitializeIkGeneral()
    {
        SetAvatarPose(AvatarPose.IKPose);

        VRIKUtils.CalculateInitialIKScaling(_vrik, ref _ikHandler._locomotionData);
        VRIKUtils.CalculateInitialFootsteps(_vrik, ref _ikHandler._locomotionData);
        _solver.Initiate(_vrik.transform); // initiate a second time

        SetAvatarPose(AvatarPose.Initial);

        VRIKUtils.ApplyScaleToVRIK(_vrik, _ikHandler._locomotionData, 1f);
        _vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdateGeneral);
        _vrik.onPostSolverUpdate.AddListener(OnPostSolverUpdateGeneral);
    }

    #endregion

    #region Public Methods

    public Vector3 GetPlayerPosition()
    {
        if (!MetaPort.Instance.isUsingVr)
            return transform.position;

        Vector3 vrPosition = _vrCamera.transform.position;
        vrPosition.y = transform.position.y;
        return vrPosition;
    }

    public Quaternion GetPlayerRotation()
    {
        if (!MetaPort.Instance.isUsingVr)
            return transform.rotation;

        Vector3 vrForward = _vrCamera.transform.forward;
        vrForward.y = 0f;
        return Quaternion.LookRotation(vrForward, Vector3.up);
    }

    #endregion

    #region VRIK Solver Events General

    private void OnPreSolverUpdateGeneral()
    {
        if (_solver.IKPositionWeight < 0.9f)
            return;

        Vector3 hipPos = _hipTransform.position;
        Quaternion hipRot = _hipTransform.rotation;

        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (var i = 0; i < _humanPose.muscles.Length; i++)
        {
            switch (BodySystem.boneResetMasks[i])
            {
                case BodySystem.BoneResetMask.Never:
                    break;
                case BodySystem.BoneResetMask.Spine:
                    _humanPose.muscles[i] *= 1 - _solver.spine.pelvisPositionWeight;
                    break;
                case BodySystem.BoneResetMask.LeftArm:
                    _humanPose.muscles[i] *= 1 - _solver.leftArm.positionWeight;
                    break;
                case BodySystem.BoneResetMask.RightArm:
                    _humanPose.muscles[i] *= 1 - _solver.rightArm.positionWeight;
                    break;
                case BodySystem.BoneResetMask.LeftLeg:
                    _humanPose.muscles[i] *= 1 - _solver.leftLeg.positionWeight;
                    break;
                case BodySystem.BoneResetMask.RightLeg:
                    _humanPose.muscles[i] *= 1 - _solver.rightLeg.positionWeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _humanPoseHandler.SetHumanPose(ref _humanPose);

        _hipTransform.position = hipPos;
        _hipTransform.rotation = hipRot;
    }

    // "NetIk Pass", or "Additional Humanoid Pass" hack
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

        for (var i = 0; i < BodySystem.boneResetMasks.Length; i++)
        {
            if (BodySystem.boneResetMasks[i] != BodySystem.BoneResetMask.Never)
                _humanPose.muscles[i] = value;
        }

        _humanPose.bodyPosition = Vector3.up;
        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    private void SetMusclesToPose(float[] muscles)
    {
        _humanPoseHandler.GetHumanPose(ref _humanPose);

        for (var i = 0; i < BodySystem.boneResetMasks.Length; i++)
        {
            if (BodySystem.boneResetMasks[i] != BodySystem.BoneResetMask.Never)
                _humanPose.muscles[i] = muscles[i];
        }

        _humanPose.bodyPosition = Vector3.up;
        _humanPose.bodyRotation = Quaternion.identity;
        _humanPoseHandler.SetHumanPose(ref _humanPose);
    }

    #endregion
}