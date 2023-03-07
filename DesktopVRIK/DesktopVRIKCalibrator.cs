using ABI.CCK.Components;
using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRIK;

public class DesktopVRIKCalibrator
{
    // Settings
    public bool Setting_UseVRIKToes = true;
    public bool Setting_FindUnmappedToes = true;

    // Avatar Component References
    public CVRAvatar avatar;
    public Animator animator;
    public Transform avatarTransform;
    public VRIK vrik;
    public LookAtIK lookAtIK;

    // Calibrated Values
    public float
        initialFootDistance,
        initialStepThreshold,
        initialStepHeight;

    // Calibration Internals
    bool fixTransformsRequired;
    Vector3 leftKneeNormal, rightKneeNormal;
    HumanPose initialHumanPose;
    HumanPoseHandler humanPoseHandler;

    // Traverse
    IKSystem ikSystem;
    PlayerSetup playerSetup;
    Traverse
        _vrikTraverse,
        _lookIKTraverse,
        _avatarTraverse,
        _animatorManagerTraverse,
        _poseHandlerTraverse,
        _avatarRootHeightTraverse;

    public DesktopVRIKCalibrator()
    {
        // Get base game scripts.
        ikSystem = IKSystem.Instance;
        playerSetup = PlayerSetup.Instance;

        // Get traverse to private shit in iksystem.
        _vrikTraverse = Traverse.Create(ikSystem).Field("_vrik");
        _avatarTraverse = Traverse.Create(ikSystem).Field("_avatar");
        _animatorManagerTraverse = Traverse.Create(ikSystem).Field("_animatorManager");
        _poseHandlerTraverse = Traverse.Create(ikSystem).Field("_poseHandler");
        _avatarRootHeightTraverse = Traverse.Create(ikSystem).Field("_avatarRootHeight");

        // Get traverse to private shit in playersetup.
        _lookIKTraverse = Traverse.Create(playerSetup).Field("lookIK");
    }

    public void CalibrateDesktopVRIK()
    {
        // Scan avatar for issues/references
        ScanAvatarForCalibration();
        // Prepare CVR IKSystem for external VRIK
        PrepareIKSystem();

        // Add VRIK and configure
        PrepareAvatarVRIK();

        Initialize();

        PostInitialize();
    }

    private void Initialize()
    {
        // Calculate bend normals with motorcycle pose
        SetHumanPose(0f);
        VRIKUtils.CalculateKneeBendNormals(vrik, out leftKneeNormal, out rightKneeNormal);

        // Calculate initial IK scaling values with IKPose
        SetAvatarIKPose(true);
        VRIKUtils.CalculateInitialIKScaling(vrik, out initialFootDistance, out initialStepThreshold, out initialStepHeight);

        // Setup HeadIK target & calculate initial footstep values
        SetupDesktopHeadIKTarget();

        // Initiate VRIK manually
        VRIKUtils.InitiateVRIKSolver(vrik);

        // Return avatar to original pose
        SetAvatarIKPose(false);
    }

    private void PostInitialize()
    {
        VRIKUtils.ApplyScaleToVRIK
        (
            vrik,
            initialFootDistance,
            initialStepThreshold,
            initialStepHeight,
            1f
        );
        VRIKUtils.ApplyKneeBendNormals(vrik, leftKneeNormal, rightKneeNormal);
        vrik.onPreSolverUpdate.AddListener(new UnityAction(DesktopVRIK.Instance.OnPreSolverUpdate));
    }

    private void ScanAvatarForCalibration()
    {
        // Find required avatar components
        avatar = playerSetup._avatar.GetComponent<CVRAvatar>();
        animator = avatar.GetComponent<Animator>();
        avatarTransform = avatar.transform;
        lookAtIK = _lookIKTraverse.GetValue<LookAtIK>();

        // Apply some fixes for weird setups
        fixTransformsRequired = !animator.enabled;

        // Center avatar local position
        avatarTransform.localPosition = Vector3.zero;

        // Create a new human pose handler and dispose the old one
        humanPoseHandler?.Dispose();
        humanPoseHandler = new HumanPoseHandler(animator.avatar, avatarTransform);
        // Store original human pose
        humanPoseHandler.GetHumanPose(ref initialHumanPose);
    }

    private void PrepareIKSystem()
    {
        // Get the animator manager and human pose handler
        var animatorManager = _animatorManagerTraverse.GetValue<CVRAnimatorManager>();
        var ikHumanPoseHandler = _poseHandlerTraverse.GetValue<HumanPoseHandler>();

        // Store the avatar component
        _avatarTraverse.SetValue(avatar);

        // Set the animator for the IK system
        ikSystem.animator = animator;
        animatorManager.SetAnimator(ikSystem.animator, ikSystem.animator.runtimeAnimatorController);

        // Set the avatar height float
        _avatarRootHeightTraverse.SetValue(ikSystem.vrPlaySpace.transform.InverseTransformPoint(avatarTransform.position).y);

        // Create a new human pose handler and dispose the old one
        ikHumanPoseHandler?.Dispose();
        ikHumanPoseHandler = new HumanPoseHandler(ikSystem.animator.avatar, avatarTransform);
        _poseHandlerTraverse.SetValue(ikHumanPoseHandler);

        // Find valid human bones
        IKSystem.BoneExists.Clear();
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone != HumanBodyBones.LastBone)
            {
                IKSystem.BoneExists.Add(bone, ikSystem.animator.GetBoneTransform(bone) != null);
            }
        }

        // Prepare BodySystem for calibration
        BodySystem.TrackingLeftArmEnabled = false;
        BodySystem.TrackingRightArmEnabled = false;
        BodySystem.TrackingLeftLegEnabled = false;
        BodySystem.TrackingRightLegEnabled = false;
        BodySystem.TrackingPositionWeight = 0f;
    }

    private void PrepareAvatarVRIK()
    {
        // Add and configure VRIK
        vrik = avatar.gameObject.AddComponentIfMissing<VRIK>();
        vrik.AutoDetectReferences();

        VRIKUtils.ConfigureVRIKReferences(vrik, Setting_UseVRIKToes, Setting_FindUnmappedToes, out bool foundUnmappedToes);

        // Fix animator issue or non-human mapped toes
        vrik.fixTransforms = fixTransformsRequired || foundUnmappedToes;

        // Default solver settings
        vrik.solver.locomotion.weight = 0f;
        vrik.solver.locomotion.angleThreshold = 30f;
        vrik.solver.locomotion.maxLegStretch = 1f;
        vrik.solver.spine.minHeadHeight = 0f;
        vrik.solver.IKPositionWeight = 1f;
        vrik.solver.spine.chestClampWeight = 0f;
        vrik.solver.spine.maintainPelvisPosition = 0f;

        // Body leaning settings
        vrik.solver.spine.neckStiffness = 0.0001f;
        vrik.solver.spine.bodyPosStiffness = 1f;
        vrik.solver.spine.bodyRotStiffness = 0.2f;

        // Disable locomotion
        vrik.solver.locomotion.velocityFactor = 0f;
        vrik.solver.locomotion.maxVelocity = 0f;
        vrik.solver.locomotion.rootSpeed = 1000f;

        // Disable chest rotation by hands
        vrik.solver.spine.rotateChestByHands = 0f;

        // Prioritize LookAtIK
        vrik.solver.spine.headClampWeight = 0.2f;

        // Disable going on tippytoes
        vrik.solver.spine.positionWeight = 0f;
        vrik.solver.spine.rotationWeight = 1f;

        // Tell IKSystem about new VRIK
        _vrikTraverse.SetValue(vrik);
    }

    private void SetupDesktopHeadIKTarget()
    {
        // Lazy HeadIKTarget calibration
        if (vrik.solver.spine.headTarget == null)
        {
            vrik.solver.spine.headTarget = new GameObject("Head IK Target").transform;
        }
        vrik.solver.spine.headTarget.parent = vrik.references.head;
        vrik.solver.spine.headTarget.localPosition = Vector3.zero;
        vrik.solver.spine.headTarget.localRotation = Quaternion.identity;
    }

    private void SetAvatarIKPose(bool enforceTPose)
    {
        int ikposeLayerIndex = animator.GetLayerIndex("IKPose");
        int locoLayerIndex = animator.GetLayerIndex("Locomotion/Emotes");

        // Use custom IKPose if found.
        if (ikposeLayerIndex != -1 && locoLayerIndex != -1)
        {
            animator.SetLayerWeight(ikposeLayerIndex, enforceTPose ? 1f : 0f);
            animator.SetLayerWeight(locoLayerIndex, enforceTPose ? 0f : 1f);
            animator.Update(0f);
            return;
        }

        // Otherwise use DesktopVRIK IKPose & revert afterwards.
        if (enforceTPose)
        {
            SetHumanPose(1f);
        }
        else
        {
            humanPoseHandler.SetHumanPose(ref initialHumanPose);
        }
    }

    private void SetHumanPose(float ikPoseWeight = 1f)
    {
        humanPoseHandler.GetHumanPose(ref ikSystem.humanPose);
        for (int i = 0; i < ikSystem.humanPose.muscles.Length; i++)
        {
            float weight = ikPoseWeight * IKPoseMuscles[i];
            IKSystem.Instance.ApplyMuscleValue((MuscleIndex)i, weight, ref ikSystem.humanPose.muscles);
        }
        ikSystem.humanPose.bodyRotation = Quaternion.identity;
        humanPoseHandler.SetHumanPose(ref ikSystem.humanPose);
    }

    private static readonly float[] IKPoseMuscles = new float[]
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
}