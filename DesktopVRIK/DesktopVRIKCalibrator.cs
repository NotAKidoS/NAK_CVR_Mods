using ABI_RC.Core.Base;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using NAK.DesktopVRIK.VRIKHelper;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.DesktopVRIK;

internal class DesktopVRIKCalibrator
{
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

    readonly DesktopVRIKSystem ikSystem;

    // Avatar Components
    Animator _animator;

    // Calibration Objects
    public HumanPoseHandler _humanPoseHandler;
    public HumanPose _humanPose;
    public HumanPose _humanPoseInitial;

    // Animator Info
    int _animLocomotionLayer = -1;
    int _animIKPoseLayer = -1;

    internal DesktopVRIKCalibrator()
    {
        ikSystem = DesktopVRIKSystem.Instance;
        BoneExists = new Dictionary<HumanBodyBones, bool>();
    }

    public void ApplyNetIKPass()
    {
        Transform hipTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
        Vector3 hipPosition = hipTransform.position;
        Quaternion hipRotation = hipTransform.rotation;

        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.SetHumanPose(ref _humanPose);

        hipTransform.position = hipPosition;
        hipTransform.rotation = hipRotation;
    }

    public void CalibrateDesktopVRIK(Animator animator)
    {
        ScanAvatar(animator);
        SetupVRIK();
        CalibrateVRIK();
        ConfigureVRIK();
    }

    void ScanAvatar(Animator animator)
    {
        // Find required avatar components
        _animator = animator;
        ikSystem.avatarTransform = animator.gameObject.transform;
        ikSystem.avatarLookAtIK = animator.gameObject.GetComponent<LookAtIK>();

        // Get animator layer inticies
        _animIKPoseLayer = _animator.GetLayerIndex("IKPose");
        _animLocomotionLayer = _animator.GetLayerIndex("Locomotion/Emotes");

        // Dispose and create new _humanPoseHandler
        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(_animator.avatar, ikSystem.avatarTransform);

        // Get initial human poses
        _humanPoseHandler.GetHumanPose(ref _humanPose);
        _humanPoseHandler.GetHumanPose(ref _humanPoseInitial);

        ikSystem.calibrationData.Clear();

        // Dumb fix for rare upload issue
        ikSystem.calibrationData.FixTransformsRequired = !_animator.enabled;

        // Find available HumanoidBodyBones
        BoneExists.Clear();
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone != HumanBodyBones.LastBone)
            {
                BoneExists.Add(bone, _animator.GetBoneTransform(bone) != null);
            }
        }
    }

    void SetupVRIK()
    {
        // Add and configure VRIK
        ikSystem.avatarVRIK = ikSystem.avatarTransform.AddComponentIfMissing<VRIK>();
        ikSystem.avatarVRIK.AutoDetectReferences();
        ikSystem.cachedSolver = new CachedSolver(ikSystem.avatarVRIK.solver);

        // Why do I love to overcomplicate things?
        VRIKUtils.ConfigureVRIKReferences(ikSystem.avatarVRIK, DesktopVRIK.EntryUseVRIKToes.Value);
        VRIKConfigurator.ApplyVRIKConfiguration(ikSystem.cachedSolver, VRIKConfigurations.DesktopVRIKConfiguration());

        // Fix potential animator issue
        ikSystem.avatarVRIK.fixTransforms = ikSystem.calibrationData.FixTransformsRequired;
    }

    void CalibrateVRIK()
    {
        SetAvatarPose(AvatarPose.Default);

        // Calculate bend normals with motorcycle pose
        VRIKUtils.CalculateKneeBendNormals(ikSystem.avatarVRIK, ref ikSystem.calibrationData);

        SetAvatarPose(AvatarPose.IKPose);

        // Calculate initial IK scaling values with IKPose
        VRIKUtils.CalculateInitialIKScaling(ikSystem.avatarVRIK, ref ikSystem.calibrationData);

        // Calculate initial Footstep positions
        VRIKUtils.CalculateInitialFootsteps(ikSystem.avatarVRIK, ref ikSystem.calibrationData);

        // Setup HeadIKTarget
        VRIKUtils.SetupHeadIKTarget(ikSystem.avatarVRIK);

        // Initiate VRIK manually
        VRIKUtils.InitiateVRIKSolver(ikSystem.avatarVRIK);

        SetAvatarPose(AvatarPose.Initial);
    }

    void ConfigureVRIK()
    {
        ikSystem.OnSetupIKScaling(1f);

        VRIKUtils.ApplyKneeBendNormals(ikSystem.avatarVRIK, ikSystem.calibrationData);

        ikSystem.avatarVRIK.onPreSolverUpdate.AddListener(new UnityAction(ikSystem.OnPreSolverUpdate));
        ikSystem.avatarVRIK.onPostSolverUpdate.AddListener(new UnityAction(ikSystem.OnPostSolverUpdate));
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
        _animator.SetLayerWeight(_animIKPoseLayer, customIKPoseLayerWeight);
        _animator.SetLayerWeight(_animLocomotionLayer, locomotionLayerWeight);
        _animator.Update(0f);
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
