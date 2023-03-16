using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.Melons.FuckCohtml.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Postfix_PlayerSetup_Start()
    {
        FuckCohtml.ToggleMetrics(FuckCohtmlMod.EntryDisableMetrics.Value);
        FuckCohtml.ToggleCoreUpdates(FuckCohtmlMod.EntryDisableCoreUpdates.Value);
    }
}

class CVR_MenuManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static void Postfix_CVR_MenuManager_ToggleQuickMenu(bool show)
    {
        if (!FuckCohtmlMod.EntryDisableCoreUpdates.Value) return;
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
        if (!FuckCohtmlMod.EntryDisableMetrics.Value) return;
        if (show)
        {
            CVR_MenuManager.Instance.SendCoreUpdate();
            ViewManager.Instance.UpdateMetrics();
        }
    }
}