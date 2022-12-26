using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace DesktopVRIK;

public class DesktopVRIK : MonoBehaviour
{
    public static DesktopVRIK Instance;

    public bool Setting_Enabled;
    public bool Setting_EmulateVRChatHipMovement;
    public bool Setting_EmoteVRIK;
    public bool Setting_EmoteLookAtIK;

    void Start()
    {
        Instance = this;
    }

    public void OnPreSolverUpdate()
    {
        //Reset avatar offset (VRIK will literally make you walk away from root otherwise)
        IKSystem.vrik.transform.localPosition = Vector3.zero;
        IKSystem.vrik.transform.localRotation = Quaternion.identity;

        //VRChat hip movement emulation
        if (Setting_EmulateVRChatHipMovement)
        {
            float angle = PlayerSetup.Instance.desktopCamera.transform.localEulerAngles.x;
            angle = (angle > 180) ? angle - 360 : angle;
            float weight = (1 - MovementSystem.Instance.movementVector.magnitude);
            Quaternion rotation = Quaternion.AngleAxis(angle * weight, IKSystem.Instance.avatar.transform.right);
            IKSystem.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Head, rotation);
        }
    }

    public void CalibrateDesktopVRIK(CVRAvatar avatar)
    {
        //Generic VRIK calibration shit

        IKSystem.vrik.fixTransforms = false;
        IKSystem.vrik.solver.plantFeet = false;
        IKSystem.vrik.solver.locomotion.weight = 1f;
        IKSystem.vrik.solver.locomotion.angleThreshold = 30f;
        IKSystem.vrik.solver.locomotion.maxLegStretch = 0.75f;
        //nuke weights
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

        //centerEyeAnchor now is head bone
        Transform headAnchor = FindIKTarget(IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Head));
        IKSystem.Instance.headAnchorPositionOffset = Vector3.zero;
        IKSystem.Instance.headAnchorRotationOffset = Vector3.zero;
        IKSystem.Instance.ApplyAvatarScaleToIk(avatar.viewPosition.y);
        BodySystem.TrackingLeftArmEnabled = false;
        BodySystem.TrackingRightArmEnabled = false;
        BodySystem.TrackingLeftLegEnabled = false;
        BodySystem.TrackingRightLegEnabled = false;
        IKSystem.vrik.solver.IKPositionWeight = 0f;
        IKSystem.vrik.enabled = false;
        //Calibrate HeadIKOffset
        VRIKCalibrator.CalibrateHead(IKSystem.vrik, headAnchor, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);
        IKSystem.vrik.enabled = true;
        IKSystem.vrik.solver.IKPositionWeight = 1f;
        IKSystem.vrik.solver.spine.maintainPelvisPosition = 0f;
        if (IKSystem.vrik != null)
        {
            IKSystem.vrik.onPreSolverUpdate.AddListener(new UnityAction(this.OnPreSolverUpdate));
        }
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