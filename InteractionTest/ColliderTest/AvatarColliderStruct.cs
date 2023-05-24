using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct AvatarTransforms
{
    public Transform root;
    public Transform hips;
    public Transform spine;
    public Transform chest;
    public Transform neck;
    public Transform head;
    public Transform leftShoulder;
    public Transform leftUpperArm;
    public Transform leftLowerArm;
    public Transform leftHand;
    public Transform rightShoulder;
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform rightHand;
    public Transform leftUpperLeg;
    public Transform leftLowerLeg;
    public Transform leftFoot;
    public Transform rightUpperLeg;
    public Transform rightLowerLeg;
    public Transform rightFoot;

    public AvatarTransforms GetAvatarTransforms(Animator animator)
    {
        AvatarTransforms result = new AvatarTransforms()
        {
            root = animator.transform,
            hips = animator.GetBoneTransform(HumanBodyBones.Hips),
            spine = animator.GetBoneTransform(HumanBodyBones.Spine),
            chest = animator.GetBoneTransform(HumanBodyBones.Chest),
            neck = animator.GetBoneTransform(HumanBodyBones.Neck),
            head = animator.GetBoneTransform(HumanBodyBones.Head),
            leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder),
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
            leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand),
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder),
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
            rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand),
            leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
            leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot),
            rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),
            rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot),
        };

        return result;
    }
}