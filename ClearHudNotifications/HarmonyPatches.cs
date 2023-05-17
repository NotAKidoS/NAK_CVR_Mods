using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using cohtml;
using HarmonyLib;
using static ABI_RC.Core.Networking.AuthManager;

namespace NAK.ClearHudNotifications.HarmonyPatches;

internal static class CohtmlHudPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CohtmlHud), nameof(CohtmlHud.ViewDropText), new Type[] { typeof(string), typeof(string), typeof(string) })]
    private static bool Prefix_CohtmlHud_ViewDropText(string cat, string headline, string small, ref CohtmlHud __instance)
    {
        if (!headline.Contains(username)) return true; // we only want our username notification

        if (small == "A user has joined your Instance." && !MetaPort.Instance.settings.GetSettingsBool("HUDCustomizationPlayerJoins", false))
        {
            ClearHudNotifications.ClearNotifications();
            return false;
        }

        // clears buffer
        if (__instance._isReady) __instance.hudView.View.TriggerEvent("DisplayHudMessageImmediately", cat, headline, small, 3);
        return false;
    }
}