using UnityEngine;
using RootMotion.FinalIK;

namespace NAK.InteractionTest;

public class AutoArmIK : MonoBehaviour
{
    public bool calibrateOnStart = true;
    public Transform leftHand, rightHand;

    Animator animator;
    ArmIK leftArmIK, rightArmIK;

    void Start()
    {
        if (calibrateOnStart) CalibrateArmIK();
    }

    public void CalibrateArmIK()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on the avatar.");
            return;
        }

        leftArmIK = gameObject.AddComponent<ArmIK>();
        leftArmIK.solver.isLeft = true;
        rightArmIK = gameObject.AddComponent<ArmIK>();
        rightArmIK.solver.isLeft = false;

        CreateHandTarget(HumanBodyBones.LeftHand, ref leftHand);
        CreateHandTarget(HumanBodyBones.RightHand, ref rightHand);

        leftArmIK.solver.arm.target = leftHand;
        rightArmIK.solver.arm.target = rightHand;

        SetArmIKChain(leftArmIK, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        SetArmIKChain(rightArmIK, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);

        leftArmIK.solver.IKPositionWeight = 1f;
        rightArmIK.solver.IKPositionWeight = 1f;
    }

    private void CreateHandTarget(HumanBodyBones bone, ref Transform handTarget)
    {
        var boneTransform = animator.GetBoneTransform(bone);
        var handGO = new GameObject($"{boneTransform.name} Target");
        handTarget = handGO.transform;
        handTarget.position = boneTransform.position;

        Vector3 sourceYWorld = boneTransform.TransformDirection(Vector3.up);
        handTarget.rotation = Quaternion.LookRotation(boneTransform.forward, sourceYWorld);
    }

    private void SetArmIKChain(ArmIK armIK, HumanBodyBones shoulder, HumanBodyBones upperArm, HumanBodyBones lowerArm, HumanBodyBones hand)
    {
        armIK.solver.SetChain(
            animator.GetBoneTransform(HumanBodyBones.Chest),
            animator.GetBoneTransform(shoulder),
            animator.GetBoneTransform(upperArm),
            animator.GetBoneTransform(lowerArm),
            animator.GetBoneTransform(hand),
            animator.transform
        );
    }
}