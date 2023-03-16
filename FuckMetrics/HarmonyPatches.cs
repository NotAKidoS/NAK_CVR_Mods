using ABI_RC.Core.InteractionSystem;
using HarmonyLib;

namespace NAK.Melons.FuckMetrics.HarmonyPatches;

class CVR_MenuManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static void Postfix_CVR_MenuManager_ToggleQuickMenu(bool show)
    {
        var disableMetrics = FuckMetricsMod.EntryDisableMetrics.Value;
        var disableCoreUpdates = FuckMetricsMod.EntryDisableCoreUpdates.Value;

        if (disableMetrics == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleMetrics(show);
        }
        else if (disableMetrics == FuckMetricsMod.SettingState.Disabled && show)
        {
            ViewManager.Instance.UpdateMetrics();
        }

        if (disableCoreUpdates == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleCoreUpdates(show);
        }
        else if (disableCoreUpdates == FuckMetricsMod.SettingState.Disabled && show)
        {
            CVR_MenuManager.Instance.SendCoreUpdate();
        }
    }
}

class ViewManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "UiStateToggle", new Type[] { typeof(bool) })]
    private static void Postfix_ViewManager_UiStateToggle(bool show)
    {
        var disableMetrics = FuckMetricsMod.EntryDisableMetrics.Value;
        var disableCoreUpdates = FuckMetricsMod.EntryDisableCoreUpdates.Value;

        if (disableMetrics == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleMetrics(show);
        }
        else if (disableMetrics == FuckMetricsMod.SettingState.Disabled && show)
        {
            ViewManager.Instance.UpdateMetrics();
        }

        if (disableCoreUpdates == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleCoreUpdates(show);
        }
        else if (disableCoreUpdates == FuckMetricsMod.SettingState.Disabled && show)
        {
            CVR_MenuManager.Instance.SendCoreUpdate();
        }
    }
}