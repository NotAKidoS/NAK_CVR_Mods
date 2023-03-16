using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;

namespace NAK.Melons.FuckMetrics
{
    public static class FuckMetrics
    {
        public static void ToggleMetrics(bool disable)
        {
            var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == "UpdateMetrics").Job;
            if (!disable && job == null)
            {
                SchedulerSystem.AddJob(new SchedulerSystem.Job(ViewManager.Instance.UpdateMetrics), 0f, 0.5f, -1);
            }
            else if (disable && job != null)
            {
                SchedulerSystem.RemoveJob(job);
            }
        }

        public static void ToggleCoreUpdates(bool disable)
        {
            var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == "SendCoreUpdate").Job;
            if (!disable && job == null)
            {
                SchedulerSystem.AddJob(new SchedulerSystem.Job(CVR_MenuManager.Instance.SendCoreUpdate), 0f, 0.1f, -1);
            }
            else if (disable && job != null)
            {
                SchedulerSystem.RemoveJob(job);
            }
        }
    }
}
