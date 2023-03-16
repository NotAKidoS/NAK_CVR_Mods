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
        FuckMetrics.ToggleMetrics(FuckMetricsMod.EntryDisableMetrics.Value);
        FuckMetrics.ToggleCoreUpdates(FuckMetricsMod.EntryDisableCoreUpdates.Value);
    }
}

class CVR_MenuManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static void Postfix_CVR_MenuManager_ToggleQuickMenu(bool show)
    {
        if (!FuckMetricsMod.EntryDisableCoreUpdates.Value) return;
        if (show)
        {
            CVR_MenuManager.Instance.SendCoreUpdate();
            ViewManager.Instance.UpdateMetrics();
        }
    }
}

class ViewManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "UiStateToggle", new Type[] { typeof(bool) })]
    private static void Postfix_ViewManager_UiStateToggle(bool show)
    {
        if (!FuckMetricsMod.EntryDisableMetrics.Value) return;
        if (show)
        {
            CVR_MenuManager.Instance.SendCoreUpdate();
            ViewManager.Instance.UpdateMetrics();
        }
    }
}