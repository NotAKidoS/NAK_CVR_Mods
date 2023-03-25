using UnityEngine;
using UnityEngine.Playables;

namespace NAK.Melons.BadAnimatorFix;

public class BadAnimatorFix : MonoBehaviour
{
    private const float StateLimit = 20f;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable() => BadAnimatorFixManager.Add(this);
    private void OnDisable() => BadAnimatorFixManager.Remove(this);

    public void AttemptRewindAnimator()
    {
        bool rewound = false;

        if (animator != null && animator.isActiveAndEnabled)
        {
            for (int layerIndex = 0; layerIndex < animator.layerCount; layerIndex++)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(layerIndex);

                // Skip if state doesn't loop or if mid-transition
                if (!stateInfo.loop || transitionInfo.fullPathHash != 0) continue;

                // Skip if state hasn't looped enough
                if (stateInfo.normalizedTime > StateLimit)
                {
                    float rewindOffset = (stateInfo.normalizedTime % 1f) + 10f;
                    animator.Play(stateInfo.fullPathHash, layerIndex, rewindOffset);
                    rewound = true;
                }
            }

            if (rewound)
            {
                PlayableExtensions.SetTime<Playable>(animator.playableGraph.GetRootPlayable(0), 0);
            }
        }

        if (BadAnimatorFixMod.EntryLogging.Value)
        {
            string message = rewound ? $"Rewound animator and playable {animator}." : $"Animator did not meet criteria to rewind {animator}.";
            BadAnimatorFixMod.Logger.Msg(message);
        }
    }
}