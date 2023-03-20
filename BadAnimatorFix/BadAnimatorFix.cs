using UnityEngine;
using UnityEngine.Playables;

namespace NAK.Melons.BadAnimatorFix;

public class BadAnimatorFix : MonoBehaviour
{
    private float stateLimit = 20f;
    private Animator animator;
    private Playable playable;

    private void Start()
    {
        animator = GetComponent<Animator>();
        playable = animator.playableGraph.GetRootPlayable(0);
    }

    private void Update()
    {
        if (!BadAnimatorFixMod.EntryEnabled.Value) return;
        if (playable.IsValid() && GetTime() > BadAnimatorFixMod.EntryPlayableTimeLimit.Value)
        {
            RewindAnimator();
            BadAnimatorFixMod.Logger.Msg($"Rewound animator and playable {animator}.");
        }
    }

    private double GetTime()
    {
        return PlayableExtensions.IsValid<Playable>(playable) ? PlayableExtensions.GetTime<Playable>(playable) : -1;
    }

    private void RewindAnimator()
    {
        PlayableExtensions.SetTime<Playable>(playable, 0);
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(i);
            // Skip if mid-transition
            if (transitionInfo.fullPathHash != 0) continue;
            // Skip if anim doesn't loop, or hasn't looped enough
            if (stateInfo.normalizedTime <= stateLimit) continue;
            // Rewind state, with 10f as buffer, to account for reasonable use of ExitTime
            float offset = 10f + (stateInfo.normalizedTime % 1f);
            animator.Play(stateInfo.fullPathHash, i, offset);
        }
    }

    private float GetNormalizedTime()
    {
        float time = 0f;
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            time += stateInfo.normalizedTime;
        }
        return time;
    }

    private float GetMaxNormalizedTime()
    {
        float time = 0f;
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            if (time < stateInfo.normalizedTime)
                time = stateInfo.normalizedTime;
        }
        return time;
    }
}
