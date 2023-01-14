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

    public static bool Setting_Enabled,
        Setting_EnforceViewPosition,
        Setting_EmoteVRIK,
        Setting_EmoteLookAtIK;
    public static float Setting_EmulateVRChatHipMovementWeight;

    public Transform viewpoint;
    public Vector3 initialCamPos;

    Transform headIKTarget;
    Transform avatarHeadBone;

    void Start()
    {
        Instance = this;
        // create the shared Head IK Target
        headIKTarget = new GameObject("[DesktopVRIK] Head IK Target").transform;
        headIKTarget.parent = PlayerSetup.Instance.transform;
        headIKTarget.localPosition = new Vector3(0f, 1.8f, 0f);
        headIKTarget.localRotation = Quaternion.identity;
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
        PlayerSetup.Instance.desktopCamera.transform.localPosition = initialCamPos;
    }

    public void AlternativeOnPreSolverUpdate()
    {
        //this order matters, rotation offset will be choppy if avatar is not cenetered first

        if (headIKTarget != null && avatarHeadBone != null)
        {
            headIKTarget.position = new Vector3(headIKTarget.position.x, avatarHeadBone.position.y, headIKTarget.position.z);
        }

        //Reset avatar offset (VRIK will literally make you walk away from root otherwise)
        IKSystem.vrik.transform.localPosition = Vector3.zero;
        IKSystem.vrik.transform.localRotation = Quaternion.identity;

        //VRChat hip movement emulation
        if (Setting_EmulateVRChatHipMovementWeight != 0)
        {
            float angle = PlayerSetup.Instance.desktopCamera.transform.localEulerAngles.x;
            if (angle > 180) angle -= 360;
            float leanAmount = angle * (1 - MovementSystem.Instance.movementVector.magnitude) * Setting_EmulateVRChatHipMovementWeight;
            Quaternion rotation = Quaternion.AngleAxis(leanAmount, IKSystem.Instance.avatar.transform.right);
            IKSystem.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Head, rotation);
        }

        IKSystem.vrik.solver.plantFeet = true;
    }

    public Animator animator;
    //public Quaternion originalRotation;
    public RuntimeAnimatorController runtimeAnimatorController;

    public VRIK AlternativeCalibration(CVRAvatar avatar)
    {
        animator = avatar.GetComponent<Animator>();
        avatarHeadBone = animator.GetBoneTransform(HumanBodyBones.Head);

        //Stuff to make bad armatures work (Fuck you Default Robot Kyle)
        avatar.transform.localPosition = Vector3.zero;
        //originalRotation = avatar.transform.rotation;
        //avatar.transform.rotation = Quaternion.identity;

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

        //Generic VRIK calibration shit
        VRIK vrik = avatar.gameObject.AddComponent<VRIK>();
        vrik.AutoDetectReferences();

        vrik.fixTransforms = true;
        vrik.solver.plantFeet = false;
        vrik.solver.locomotion.weight = 0f;
        vrik.solver.locomotion.angleThreshold = 30f;
        vrik.solver.locomotion.maxLegStretch = 0.75f;
        //nuke weights
        vrik.solver.spine.headClampWeight = 0f;
        vrik.solver.spine.minHeadHeight = 0f;
        //vrik.solver.spine.pelvisPositionWeight = 0f;
        vrik.solver.leftArm.positionWeight = 0f;
        vrik.solver.leftArm.rotationWeight = 0f;
        vrik.solver.rightArm.positionWeight = 0f;
        vrik.solver.rightArm.rotationWeight = 0f;
        vrik.solver.leftLeg.positionWeight = 0f;
        vrik.solver.leftLeg.rotationWeight = 0f;
        vrik.solver.rightLeg.positionWeight = 0f;
        vrik.solver.rightLeg.rotationWeight = 0f;
        vrik.solver.IKPositionWeight = 0f;

        //ChilloutVR specific
        BodySystem.TrackingLeftArmEnabled = false;
        BodySystem.TrackingRightArmEnabled = false;
        BodySystem.TrackingLeftLegEnabled = false;
        BodySystem.TrackingRightLegEnabled = false;
        IKSystem.Instance.headAnchorRotationOffset = Vector3.zero;
        IKSystem.Instance.headAnchorPositionOffset = Vector3.zero;

        //Custom funky AF head ik shit
        foreach (Transform transform in headIKTarget)
        {
            if (transform.name == "Head IK Target")
            {
                Destroy(transform.gameObject);
            }
        }
        headIKTarget.position = avatarHeadBone.position;
        headIKTarget.rotation = Quaternion.identity;
        VRIKCalibrator.CalibrateHead(vrik, headIKTarget.transform, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);
        headIKTarget.localRotation = Quaternion.identity;

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
        initialCamPos = PlayerSetup.Instance.desktopCamera.transform.localPosition;
        viewpoint = avatarHeadBone.Find("LocalHeadPoint");
        ChangeViewpointHandling(Setting_EnforceViewPosition);

        if (vrik != null)
        {
            vrik.onPreSolverUpdate.AddListener(new UnityAction(this.AlternativeOnPreSolverUpdate));
        }

        //avatar.transform.rotation = originalRotation;
        IKSystem.Instance.ResetIK();

        return vrik;
    }
}