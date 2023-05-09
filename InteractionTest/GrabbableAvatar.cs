using ABI_RC.Core;
using System.Text;
using UnityEngine;

namespace NAK.InteractionTest;

internal class GrabbableAvatar : MonoBehaviour
{
    private static readonly HumanBodyBones[][] boneSequences = new[]
    {
        new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head },
        new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot },
        new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot },
        new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand },
        new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand }
    };

    private void Start()
    {
        var animator = GetComponent<Animator>();

        for (int seqIndex = 0; seqIndex < boneSequences.Length; seqIndex++)
        {
            var boneSequence = boneSequences[seqIndex];

            for (int i = 0; i < boneSequence.Length - 1; i++)
            {
                var fromBone = animator.GetBoneTransform(boneSequence[i]);
                var toBone = animator.GetBoneTransform(boneSequence[i + 1]);

                var colliderName = new StringBuilder(fromBone.name)
                    .Append("_to_")
                    .Append(toBone.name)
                    .ToString();

                CVRTools.GenerateBoneCollider(fromBone, toBone, 1f, colliderName);
            }
        }
    }
}
