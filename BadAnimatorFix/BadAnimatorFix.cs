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
        if (animator == null) return;

        bool rewound = false;
        for (int layerIndex = 0; layerIndex < animator.layerCount; layerIndex++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(layerIndex);

            bool shouldSkipState = !stateInfo.loop || transitionInfo.fullPathHash != 0;
            if (shouldSkipState) continue;

            bool shouldRewindState = stateInfo.normalizedTime >= StateLimit;
            if (shouldRewindState)
            {
                float rewindOffset = (stateInfo.normalizedTime % 1f) + 10f;
                animator.Play(stateInfo.fullPathHash, layerIndex, rewindOffset);
                rewound = true;
            }
        }

        if (rewound)
        {
            var rootPlayable = animator.playableGraph.GetRootPlayable(0);
            PlayableExtensions.SetTime<Playable>(rootPlayable, 0);

            if (BadAnimatorFixMod.EntryLogging.Value)
                BadAnimatorFixMod.Logger.Msg($"Rewound animator and playable {animator}.");
        }
    }
}