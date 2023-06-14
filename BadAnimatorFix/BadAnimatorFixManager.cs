using ABI_RC.Core.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.BadAnimatorFix;

public static class BadAnimatorFixManager
{
    static List<BadAnimatorFixer> _animatorFixers = new List<BadAnimatorFixer>();
    static int _currentIndex = 0;
    static float _checkInterval = 5f;

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
        ToggleJob(BadAnimatorFix.EntryEnabled.Value);
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
            {
                animator.gameObject.AddComponent<BadAnimatorFixer>();
            }
        }
    }

    public static void ToggleJob(bool enable)
    {
        var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == nameof(CheckNextAnimator)).Job;
        if (enable && job == null)
        {
            SchedulerSystem.AddJob(new SchedulerSystem.Job(CheckNextAnimator), 0f, _checkInterval, -1);
        }
        else if (!enable && job != null)
        {
            SchedulerSystem.RemoveJob(job);
        }
    }

    static void CheckNextAnimator()
    {
        if (_animatorFixers.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _animatorFixers.Count;

        BadAnimatorFixer currentAnimatorFix = _animatorFixers[_currentIndex];
        currentAnimatorFix.AttemptRewindAnimator();
    }
}
