using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using cohtml;
using HarmonyLib;
using UnityEngine;

namespace NAK.Melons.FuckMetrics
{
    public static class FuckMetrics
    {
        public static void ToggleMetrics(bool enable)
        {
            var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == "UpdateMetrics").Job;
            if (enable && job == null)
            {
                SchedulerSystem.AddJob(new SchedulerSystem.Job(ViewManager.Instance.UpdateMetrics), 0f, FuckMetricsMod.EntryMetricsUpdateRate.Value, -1);
            }
            else if (!enable && job != null)
            {
                SchedulerSystem.RemoveJob(job);
            }
        }

        public static void ToggleCoreUpdates(bool enable)
        {
            var job = SchedulerSystem.Instance.activeJobs.FirstOrDefault(pair => pair.Job.Method.Name == "SendCoreUpdate").Job;
            if (enable && job == null)
            {
                SchedulerSystem.AddJob(new SchedulerSystem.Job(CVR_MenuManager.Instance.SendCoreUpdate), 0f, FuckMetricsMod.EntryCoreUpdateRate.Value, -1);
            }
            else if (!enable && job != null)
            {
                SchedulerSystem.RemoveJob(job);
            }
        }

        public static void CohtmlAdvanceView(CohtmlView cohtmlView, Traverse menuOpenTraverse)
        {
            if (!FuckMetricsMod.EntryDisableCohtmlViewOnIdle.Value) return;

            // Don't execute if menu is open
            if (cohtmlView == null || menuOpenTraverse.GetValue<bool>()) return;

            // Disable cohtmlView (opening should enable)
            cohtmlView.enabled = false;

            // Death
            try
            {
                if (cohtmlView.m_UISystem != null) cohtmlView.View.Advance(cohtmlView.m_UISystem.Id, (double)Time.unscaledTime * 1000.0);
            }
            catch (Exception e)
            {
                FuckMetricsMod.Logger.Error($"An exception was thrown while calling CohtmlView.Advance(). Error message: {e.Message}");
            }
        }
    }
}
