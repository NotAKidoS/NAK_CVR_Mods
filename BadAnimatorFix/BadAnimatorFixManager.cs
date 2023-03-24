using ABI_RC.Core.IO;

namespace NAK.Melons.BadAnimatorFix;

public static class BadAnimatorFixManager
{
    public static List<BadAnimatorFix> badAnimatorFixes = new List<BadAnimatorFix>();
    private static int currentIndex = 0;

    public static void Add(BadAnimatorFix bad)
    {
        if (!badAnimatorFixes.Contains(bad))
        {
            badAnimatorFixes.Add(bad);
        }
    }

    public static void Remove(BadAnimatorFix bad)
    {
        if (badAnimatorFixes.Contains(bad))
        {
            badAnimatorFixes.Remove(bad);
        }
    }

    // Runs every 5 seconds to see if an animator has played longer than PlayableTimeLimit
    public static void CheckNextAnimator()
    {
        if (badAnimatorFixes.Count == 0) return;

        if (currentIndex >= badAnimatorFixes.Count) currentIndex = 0;

        BadAnimatorFix currentAnimatorFix = badAnimatorFixes[currentIndex];
        if (currentAnimatorFix.GetTime() > BadAnimatorFixMod.EntryPlayableTimeLimit.Value)
        {
            currentAnimatorFix.AttemptRewindAnimator();
        }

        currentIndex++;
    }   

    public static void ToggleJob(bool enable)
    {
        var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == "CheckNextAnimator").Job;
        if (enable && job == null)
        {
            SchedulerSystem.AddJob(new SchedulerSystem.Job(BadAnimatorFixManager.CheckNextAnimator), 0f, 5f, -1);
        }
        else if (!enable && job != null)
        {
            SchedulerSystem.RemoveJob(job);
        }
    }
}
