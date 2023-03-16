using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using cohtml;
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

public static class CohtmlViewPatches
{
    private static CohtmlView _quickMenuView;
    private static CohtmlView _gameMenuView;
    private static Traverse _quickMenuOpenTraverse;
    private static Traverse _gameMenuOpenTraverse;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance, ref CohtmlView ___quickMenu)
    {
        _quickMenuView = ___quickMenu;
        _quickMenuOpenTraverse = Traverse.Create(__instance).Field("_quickMenuOpen");
        SchedulerSystem.AddJob(new SchedulerSystem.Job(() => FuckMetrics.CohtmlAdvanceView(_quickMenuView, _quickMenuOpenTraverse)), 15f, 6f, -1);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance, ref CohtmlView ___gameMenuView)
    {
        _gameMenuView = ___gameMenuView;
        _gameMenuOpenTraverse = Traverse.Create(__instance).Field("_gameMenuOpen");
        SchedulerSystem.AddJob(new SchedulerSystem.Job(() => FuckMetrics.CohtmlAdvanceView(_gameMenuView, _gameMenuOpenTraverse)), 12f, 6f, -1);
    }
}