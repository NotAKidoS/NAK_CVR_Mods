using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using cohtml;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.FuckCohtml.HarmonyPatches;

public static class CohtmlViewPatches
{
    private static CohtmlView _quickMenuView;
    private static CohtmlView _gameMenuView;
    private static Traverse _quickMenuOpenTraverse;
    private static Traverse _gameMenuOpenTraverse;
    private static readonly FieldInfo m_UISystemFieldInfo = AccessTools.Field(typeof(CohtmlView), "m_UISystem");

    private static void CohtmlAdvanceView(CohtmlView cohtmlView, Traverse menuOpenTraverse)
    {
        if (!FuckCohtmlMod.EntryEnabled.Value) return;

        // Don't execute if menu is open
        if (cohtmlView == null || menuOpenTraverse.GetValue<bool>()) return;

        // Disable cohtmlView (opening should enable)
        cohtmlView.enabled = false;

        // Death
        try
        {
            CohtmlUISystem cohtmlUISystem = (CohtmlUISystem)m_UISystemFieldInfo.GetValue(cohtmlView);
            if (cohtmlUISystem != null) cohtmlView.View.Advance(cohtmlUISystem.Id, (double)Time.unscaledTime * 1000.0);
        }
        catch (Exception e)
        {
            FuckCohtmlMod.Logger.Error($"An exception was thrown while calling CohtmlView.Advance(). Error message: {e.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance, ref CohtmlView ___quickMenu)
    {
        _quickMenuView = ___quickMenu;
        _quickMenuOpenTraverse = Traverse.Create(__instance).Field("_quickMenuOpen");
        SchedulerSystem.AddJob(new SchedulerSystem.Job(() => CohtmlAdvanceView(_quickMenuView, _quickMenuOpenTraverse)), 15f, 6f, -1);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance, ref CohtmlView ___gameMenuView)
    {
        _gameMenuView = ___gameMenuView;
        _gameMenuOpenTraverse = Traverse.Create(__instance).Field("_gameMenuOpen");
        SchedulerSystem.AddJob(new SchedulerSystem.Job(() => CohtmlAdvanceView(_gameMenuView, _gameMenuOpenTraverse)), 12f, 6f, -1);
    }
}