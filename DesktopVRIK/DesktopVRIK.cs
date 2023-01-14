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
        PlayerSetup.Instance.desktopCamera.transform.localPosition = initialCamPos;
    }

    public void OnPreSolverUpdate()
    {
        //this order matters, rotation offset will be choppy if avatar is not cenetered first
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

    public void CalibrateDesktopVRIK(CVRAvatar avatar)
    {
        //ikpose layer (specified by avatar author)
        int? ikposeLayerIndex = PlayerSetup.Instance.animatorManager.GetAnimatorLayerIndex("IKPose");
        int? locoLayerIndex = PlayerSetup.Instance.animatorManager.GetAnimatorLayerIndex("Locomotion/Emotes");

        if (ikposeLayerIndex != -1)
        {
            PlayerSetup.Instance.animatorManager.SetAnimatorLayerWeight("IKPose", 1f);
            if (locoLayerIndex != -1)
            {
                PlayerSetup.Instance.animatorManager.SetAnimatorLayerWeight("Locomotion/Emotes", 0f);
            }
            IKSystem.Instance.animator.Update(0f);
        }


        //Stuff to make bad armatures work (Fuck you Default Robot Kyle)
        avatar.transform.localPosition = Vector3.zero;
        Quaternion originalRotation = avatar.transform.rotation;
        avatar.transform.rotation = Quaternion.identity;

        //Generic VRIK calibration shit

        IKSystem.vrik.fixTransforms = false;
        IKSystem.vrik.solver.plantFeet = false;
        IKSystem.vrik.solver.locomotion.weight = 0f;
        IKSystem.vrik.solver.locomotion.angleThreshold = 30f;
        IKSystem.vrik.solver.locomotion.maxLegStretch = 0.75f;
        //nuke weights
        IKSystem.vrik.AutoDetectReferences();
        IKSystem.vrik.solver.spine.headClampWeight = 0f;
        IKSystem.vrik.solver.spine.minHeadHeight = 0f;
        IKSystem.vrik.solver.leftArm.positionWeight = 0f;
        IKSystem.vrik.solver.leftArm.rotationWeight = 0f;
        IKSystem.vrik.solver.rightArm.positionWeight = 0f;
        IKSystem.vrik.solver.rightArm.rotationWeight = 0f;
        IKSystem.vrik.solver.leftLeg.positionWeight = 0f;
        IKSystem.vrik.solver.leftLeg.rotationWeight = 0f;
        IKSystem.vrik.solver.rightLeg.positionWeight = 0f;
        IKSystem.vrik.solver.rightLeg.rotationWeight = 0f;

        //ChilloutVR specific stuff

        //Find eyeoffset
        initialCamPos = PlayerSetup.Instance.desktopCamera.transform.localPosition;
        Transform headTransform = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Head);
        viewpoint = headTransform.Find("LocalHeadPoint");
        ChangeViewpointHandling(Setting_EnforceViewPosition);

        //centerEyeAnchor now is head bone
        Transform headAnchor = FindIKTarget(headTransform);
        IKSystem.Instance.headAnchorPositionOffset = Vector3.zero;
        IKSystem.Instance.headAnchorRotationOffset = headAnchor.rotation.eulerAngles; //set to head bone world rotation (Fuck you Default Robot Kyle)
        IKSystem.Instance.ApplyAvatarScaleToIk(avatar.viewPosition.y);
        BodySystem.TrackingLeftArmEnabled = false;
        BodySystem.TrackingRightArmEnabled = false;
        BodySystem.TrackingLeftLegEnabled = false;
        BodySystem.TrackingRightLegEnabled = false;
        IKSystem.vrik.solver.IKPositionWeight = 0f;
        IKSystem.vrik.enabled = false;

        //Calibrate HeadIKOffset *(this is fucked on some avatars, (Fuck you Default Robot Kyle) but setting headAnchorRotationOffset to head rotation fixes (Fuck you Default Robot Kyle))*
        VRIKCalibrator.CalibrateHead(IKSystem.vrik, headAnchor, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);

        IKSystem.vrik.enabled = true;
        IKSystem.vrik.solver.IKPositionWeight = 1f;
        IKSystem.vrik.solver.spine.maintainPelvisPosition = 0f;
        if (IKSystem.vrik != null)
        {
            IKSystem.vrik.onPreSolverUpdate.AddListener(new UnityAction(this.OnPreSolverUpdate));
        }

        if (ikposeLayerIndex != -1)
        {
            PlayerSetup.Instance.animatorManager.SetAnimatorLayerWeight("IKPose", 0f);
            if (locoLayerIndex != -1)
            {
                PlayerSetup.Instance.animatorManager.SetAnimatorLayerWeight("Locomotion/Emotes", 1f);
            }
        }

        avatar.transform.rotation = originalRotation;
        IKSystem.Instance.ResetIK();
    }

    //This is built because original build placed IK Targets on all joints.
    private static Transform FindIKTarget(Transform targetParent)
    {
        /**
            I want creators to be able to specify their own custom IK Targets, so they can move them around with animations if they want.
            We check for existing target objects, and if none are found we make our own.
            Naming scheme is parentobject name + " IK Target".
        **/
        Transform parentTransform = targetParent.transform;
        string targetName = parentTransform.name + " IK Target";

        //check for existing target
        foreach (object obj in parentTransform)
        {
            Transform childTransform = (Transform)obj;
            if (childTransform.name == targetName)
            {
                return childTransform;
            }
        }

        //create new target if none are found
        GameObject newTarget = new GameObject(targetName);
        newTarget.transform.parent = parentTransform;
        newTarget.transform.localPosition = Vector3.zero;
        newTarget.transform.localRotation = Quaternion.identity;
        return newTarget.transform;
    }
}