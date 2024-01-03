using UnityEngine;
using UnityEngine.Playables;

namespace NAK.BadAnimatorFix;

public class BadAnimatorFixer : MonoBehaviour
{
    private const float StateLimit = 20f;

    private Animator animator;

    private void Start() => animator = GetComponent<Animator>();
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
                // Skip if mid-transition
                if (transitionInfo.fullPathHash != 0) continue;
                // Skip if anim doesn't loop, or hasn't looped enough
                if (!stateInfo.loop || stateInfo.normalizedTime < StateLimit) continue;
                // Rewind state, with 10f as buffer, to account for reasonable use of ExitTime
                float offset = 10f + (stateInfo.normalizedTime % 1f);
                animator.Play(stateInfo.fullPathHash, layerIndex, offset);
                rewound = true;
            }

            if (rewound)
                PlayableExtensions.SetTime<Playable>(animator.playableGraph.GetRootPlayable(0), 0);
        }

        if (BadAnimatorFix.EntryLogging.Value)
        {
            string message = rewound ? $"Rewound animator and playable {animator}." : $"Animator did not meet criteria to rewind {animator}.";
            BadAnimatorFix.Logger.Msg(message);
        }
    }
}