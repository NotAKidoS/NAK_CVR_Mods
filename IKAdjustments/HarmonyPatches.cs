using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.IK;
using HarmonyLib;
using NAK.IKAdjustments.Systems;

namespace NAK.IKAdjustments.HarmonyPatches;

internal static class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.Start))]
    private static void Postfix_IKSystem_Start(ref IKSystem __instance)
    {
        __instance.gameObject.AddComponent<IKAdjuster>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.ResetIkSettings))]
    private static void Postfix_IKSystem_ResetIkSettings()
    {
        IKAdjuster.Instance.ResetAllOffsets();
    }
}

internal static class CVR_MenuManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), nameof(CVR_MenuManager.ToggleQuickMenu), typeof(bool))]
    private static void Postfix_CVR_MenuManager_ToggleQuickMenu(bool show)
    {
        if (show) IKAdjuster.Instance.ExitAdjustMode();
    }
}