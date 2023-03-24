using UnityEngine;
using UnityEngine.Playables;

namespace NAK.Melons.BadAnimatorFix;

public class BadAnimatorFix : MonoBehaviour
{
    private float stateLimit = 50f;
    private Animator animator;
    private Playable playable;

    void Start()
    {
        animator = GetComponent<Animator>();
        playable = animator.playableGraph.GetRootPlayable(0);
        BadAnimatorFixManager.Add(this);
    }

    void OnDestroy()
    {
        BadAnimatorFixManager.Remove(this);
    }

    public double GetTime()
    {
        return PlayableExtensions.IsValid<Playable>(playable) ? PlayableExtensions.GetTime<Playable>(playable) : -1;
    }

    public void AttemptRewindAnimator()
    {
        bool rewound = false;
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(i);
            // Skip if mid-transition
            if (transitionInfo.fullPathHash != 0) continue;
            // Skip if anim doesn't loop, or hasn't looped enough
            if (stateInfo.normalizedTime <= stateLimit) continue;
            // Rewind state, with 10f as buffer, to account for reasonable use of ExitTime
            rewound = true;
            float offset = 10f + (stateInfo.normalizedTime % 1f);
            animator.Play(stateInfo.fullPathHash, i, offset);
        }
        if (rewound)
        {
            PlayableExtensions.SetTime<Playable>(playable, 0);
            BadAnimatorFixMod.Logger.Msg($"Rewound animator and playable {animator}.");
        }
    }
}