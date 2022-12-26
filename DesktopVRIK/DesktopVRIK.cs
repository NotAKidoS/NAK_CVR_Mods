using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using MelonLoader;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

namespace DesktopVRIK;

public class NAKDesktopVRIK : MonoBehaviour
{
    public static NAKDesktopVRIK Instance;
    public VRIK vrik;

    void Start()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        //pretty much zero out VRIK trying to locomote us using autofootstep
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void CalibrateAvatarVRIK(CVRAvatar avatar)
    {
        //check if VRIK already exists, as it is an allowed component
        vrik = avatar.gameObject.GetComponent<VRIK>();
        if (vrik == null)
        {
            vrik = avatar.gameObject.AddComponent<VRIK>();
        }

        //Generic VRIK calibration shit

        vrik.fixTransforms = false;
        vrik.solver.plantFeet = false;
        vrik.solver.locomotion.weight = 1f;
        vrik.solver.locomotion.angleThreshold = 30f;
        vrik.solver.locomotion.maxLegStretch = 0.75f;
        //nuke weights
        vrik.solver.spine.headClampWeight = 0f;
        vrik.solver.spine.minHeadHeight = 0f;
        vrik.solver.leftArm.positionWeight = 0f;
        vrik.solver.leftArm.rotationWeight = 0f;
        vrik.solver.rightArm.positionWeight = 0f;
        vrik.solver.rightArm.rotationWeight = 0f;
        vrik.solver.leftLeg.positionWeight = 0f;
        vrik.solver.leftLeg.rotationWeight = 0f;
        vrik.solver.rightLeg.positionWeight = 0f;
        vrik.solver.rightLeg.rotationWeight = 0f;

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
        vrik.solver.IKPositionWeight = 0f;
        vrik.enabled = false;
        //Calibrate HeadIKOffset
        VRIKCalibrator.CalibrateHead(vrik, headAnchor, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);
        vrik.enabled = true;
        vrik.solver.IKPositionWeight = 1f;
        vrik.solver.spine.maintainPelvisPosition = 0f;
    }

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