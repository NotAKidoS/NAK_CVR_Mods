using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.Melons.DesktopVRIK;

public class DesktopVRIK : MonoBehaviour
{
    public static DesktopVRIK Instance;

    public static bool
        Setting_Enabled,
        Setting_EnforceViewPosition,
        Setting_EmoteVRIK,
        Setting_EmoteLookAtIK;

    public static float
        Setting_BodyLeanWeight = 0.5f,
        Setting_BodyAngleLimit = 0f;

    public Transform viewpoint;
    public Vector3 eyeOffset;

    void Start()
    {
        Instance = this;
    }

    public void ChangeViewpointHandling(bool enabled)
    {
        if (Setting_EnforceViewPosition == enabled) return;
        Setting_EnforceViewPosition = enabled;
        if (enabled)
        {
            PlayerSetup.Instance.desktopCamera.transform.localPosition = Vector3.zero;
            return;
        }
        PlayerSetup.Instance.desktopCamera.transform.localPosition = eyeOffset;
    }

    public void AlternativeOnPreSolverUpdate()
    {
        //this order matters, rotation offset will be choppy if avatar is not cenetered first

        DesktopVRIK_Helper.Instance?.OnUpdateVRIK();

        //Reset avatar offset (VRIK will literally make you walk away from root otherwise)
        IKSystem.vrik.transform.localPosition = Vector3.zero;
        IKSystem.vrik.transform.localRotation = Quaternion.identity;

        IKSystem.vrik.solver.plantFeet = true;
    }

    public Animator animator;

    public VRIK AlternativeCalibration(CVRAvatar avatar)
    {
        animator = avatar.GetComponent<Animator>();
        Transform avatarHeadBone = animator.GetBoneTransform(HumanBodyBones.Head);

        //Stuff to make bad armatures work (Fuck you Default Robot Kyle)
        avatar.transform.localPosition = Vector3.zero;

        //ikpose layer (specified by avatar author)
        int ikposeLayerIndex = animator.GetLayerIndex("IKPose");
        int locoLayerIndex = animator.GetLayerIndex("Locomotion/Emotes");
        if (ikposeLayerIndex != -1)
        {
            animator.SetLayerWeight(ikposeLayerIndex, 1f);
            if (locoLayerIndex != -1)
            {
                animator.SetLayerWeight(locoLayerIndex, 0f);
            }
            animator.Update(0f);
        }

        VRIK vrik = avatar.gameObject.AddComponent<VRIK>();
        vrik.AutoDetectReferences();

        //fuck toes
        vrik.references.leftToes = null;
        vrik.references.rightToes = null;

        vrik.fixTransforms = true;
        vrik.solver.plantFeet = false;
        vrik.solver.locomotion.angleThreshold = 30f;
        vrik.solver.locomotion.maxLegStretch = 0.75f;
        vrik.solver.spine.minHeadHeight = -100f;

        vrik.solver.spine.bodyRotStiffness = 0.15f;
        vrik.solver.spine.headClampWeight = 1f;
        vrik.solver.spine.maintainPelvisPosition = 1f;
        vrik.solver.spine.neckStiffness = 0f;

        vrik.solver.locomotion.weight = 0f;
        vrik.solver.spine.bodyPosStiffness = 0f;
        vrik.solver.spine.positionWeight = 0f;
        vrik.solver.spine.pelvisPositionWeight = 0f;
        vrik.solver.leftArm.positionWeight = 0f;
        vrik.solver.leftArm.rotationWeight = 0f;
        vrik.solver.rightArm.positionWeight = 0f;
        vrik.solver.rightArm.rotationWeight = 0f;
        vrik.solver.leftLeg.positionWeight = 0f;
        vrik.solver.leftLeg.rotationWeight = 0f;
        vrik.solver.rightLeg.positionWeight = 0f;
        vrik.solver.rightLeg.rotationWeight = 0f;
        vrik.solver.IKPositionWeight = 0f;

        BodySystem.TrackingLeftArmEnabled = false;
        BodySystem.TrackingRightArmEnabled = false;
        BodySystem.TrackingLeftLegEnabled = false;
        BodySystem.TrackingRightLegEnabled = false;
        BodySystem.TrackingPositionWeight = 0f;

        //Custom funky AF head ik shit
        foreach (Transform transform in DesktopVRIK_Helper.Instance.ik_HeadFollower)
        {
            if (transform.name == "Head IK Target")
            {
                Destroy(transform.gameObject);
            }
        }

        DesktopVRIK_Helper.Instance.avatar_HeadBone = avatarHeadBone;
        DesktopVRIK_Helper.Instance.ik_HeadFollower.position = avatarHeadBone.position;
        DesktopVRIK_Helper.Instance.ik_HeadFollower.rotation = Quaternion.identity;
        VRIKCalibrator.CalibrateHead(vrik, DesktopVRIK_Helper.Instance.ik_HeadFollower.transform, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);
        DesktopVRIK_Helper.Instance.ik_HeadFollower.localRotation = Quaternion.identity;

        //force immediate calibration before animator decides to fuck us
        vrik.solver.SetToReferences(vrik.references);
        vrik.solver.Initiate(vrik.transform);

        if (ikposeLayerIndex != -1)
        {
            animator.SetLayerWeight(ikposeLayerIndex, 0f);
            if (locoLayerIndex != -1)
            {
                animator.SetLayerWeight(locoLayerIndex, 1f);
            }
        }

        //Find eyeoffset
        eyeOffset = PlayerSetup.Instance.desktopCamera.transform.localPosition;
        viewpoint = avatarHeadBone.Find("LocalHeadPoint");
        ChangeViewpointHandling(Setting_EnforceViewPosition);

        //reset ikpose layer
        if (ikposeLayerIndex != -1)
        {
            animator.SetLayerWeight(ikposeLayerIndex, 0f);
            if (locoLayerIndex != -1)
            {
                animator.SetLayerWeight(locoLayerIndex, 1f);
            }
        }

        vrik?.onPreSolverUpdate.AddListener(new UnityAction(this.AlternativeOnPreSolverUpdate));

        DesktopVRIK_Helper.Instance?.OnResetIK();

        return vrik;
    }
}