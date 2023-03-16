using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.Melons.FuckMetrics.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Postfix_PlayerSetup_Start()
    {
        FuckMetrics.ToggleMetrics(FuckMetricsMod.EntryDisableMetrics.Value == FuckMetricsMod.SettingState.Enabled);
        FuckMetrics.ToggleCoreUpdates(FuckMetricsMod.EntryDisableCoreUpdates.Value == FuckMetricsMod.SettingState.Enabled);
    }
}

class CVR_MenuManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static void Postfix_CVR_MenuManager_ToggleQuickMenu(bool show)
    {
        if (FuckMetricsMod.EntryDisableCoreUpdates.Value == FuckMetricsMod.SettingState.Disabled) return;

        if (FuckMetricsMod.EntryDisableCoreUpdates.Value == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleMetrics(show);
            FuckMetrics.ToggleCoreUpdates(show);
        }
        else if (show)
        {
            ViewManager.Instance.UpdateMetrics();
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
        if (FuckMetricsMod.EntryDisableCoreUpdates.Value == FuckMetricsMod.SettingState.Disabled) return;

        if (FuckMetricsMod.EntryDisableCoreUpdates.Value == FuckMetricsMod.SettingState.MenuOnly)
        {
            FuckMetrics.ToggleMetrics(show);
            FuckMetrics.ToggleCoreUpdates(show);
        }
        else if (show)
        {
            ViewManager.Instance.UpdateMetrics();
            CVR_MenuManager.Instance.SendCoreUpdate();
        }
    }
}