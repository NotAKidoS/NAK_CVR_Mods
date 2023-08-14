using ABI_RC.Core.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.BadAnimatorFix;

public static class BadAnimatorFixManager
{
    private static List<BadAnimatorFixer> _animatorFixers = new List<BadAnimatorFixer>();
    private static readonly float _checkInterval = 5f;
    private static int _currentIndex = 0;

    public static void Add(BadAnimatorFixer bad)
    {
        if (!_animatorFixers.Contains(bad))
            _animatorFixers.Add(bad);
    }

    public static void Remove(BadAnimatorFixer bad)
    {
        if (_animatorFixers.Contains(bad))
            _animatorFixers.Remove(bad);
    }

    public static void OnPlayerLoaded()
    {
        SchedulerSystem.AddJob(new SchedulerSystem.Job(CheckNextAnimator), 0f, _checkInterval, -1);
    }

    public static void OnSceneInitialized(string sceneName)
    {
        // Get all the animators in the loaded world
        var allAnimators = SceneManager.GetSceneByName(sceneName).GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<Animator>(true));

        foreach (var animator in allAnimators)
        {
            // Ignore objects that have our "fix", this shouldn't be needed but eh
            if (!animator.TryGetComponent<BadAnimatorFixer>(out _))
                animator.gameObject.AddComponent<BadAnimatorFixer>();
        }
    }

    private static void CheckNextAnimator()
    {
        if (!BadAnimatorFix.EntryEnabled.Value)
            return;

        if (_animatorFixers.Count == 0) 
            return;

        _currentIndex = (_currentIndex + 1) % _animatorFixers.Count;
        _animatorFixers[_currentIndex].AttemptRewindAnimator();
    }
}