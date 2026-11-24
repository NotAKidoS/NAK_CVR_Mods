using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.Communications.Audio.Components;
using HarmonyLib;
using NAK.CleanPlates.Network;

namespace NAK.CleanPlates.Patches;

internal static class PlayerNameplate_Patches
{
    // Every native plate change ends here (our one refresh hook)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerNameplate), nameof(PlayerNameplate.UpdateNamePlateSettings))]
    private static bool Prefix_PlayerNameplate_UpdateNamePlateSettings(PlayerNameplate __instance)
    {
        PlateManager.RefreshRank(__instance.overheadController.playerBase);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerNameplate), nameof(PlayerNameplate.Start))]
    private static bool Prefix_PlayerNameplate_Start(ref PlayerNameplate __instance)
    {
        // Kill native plate.
        __instance.contentGo.SetActive(false);
        __instance.gameObject.SetActive(false);
        __instance.enabled = false;
        return false;
    }
}

internal static class Comms_AudioTap_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Comms_AudioTap), nameof(Comms_AudioTap.OnDataReceived))]
    private static void Prefix_Comms_AudioTap_OnDataReceived(Comms_AudioTap __instance, bool isTTS)
    {
        if (__instance is Comms_ParticipantPipeline pipeline) VoiceActivity.OnPacket(pipeline.UserId, isTTS);
    }
}

internal static class IndicatorPlate_Patches
{
    // Camera and pen indicators
    [HarmonyPrefix]
    [HarmonyPatch(typeof(IndicatorPlate), nameof(IndicatorPlate.Start))]
    private static bool Prefix_IndicatorPlate_Start(ref IndicatorPlate __instance)
    {
        // Kick off creating our own plate.
        PlateManager.OnIndicatorPlateStart(__instance);

        __instance.nameplateGameObjectWrapper.SetActive(false);
        __instance.enabled = false;
        return false;
    }
}

internal static class OverheadController_Patches
{
    // Last priority to run after TW:
    // https://github.com/TotallyWholesome/TotallyWholesomeMod/blob/19403286a812b1ab5dd7c83e2e83cbed3cda3837/TotallyWholesome/Managers/Status/StatusManager.cs#L270
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(OverheadController), nameof(OverheadController.Start))]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix_OverheadController_Start(ref OverheadController __instance)
    {
        // Instantiate our own plate. Listening here instead of PlayerNameplate
        // specifically because this is also where TW patches.
        PlateManager.OnOverheadControllerStart(__instance);
    }
}

internal static class ChatBoxBubbleBehavior_Patches
{
    // Gut the native bubble entirely, we drive our own off ChatBoxAPI.
    // Start never runs so it never registers itself or touches its materials.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatBoxBubbleBehavior), nameof(ChatBoxBubbleBehavior.Start))]
    private static bool Prefix_ChatBoxBubbleBehavior_Start(ref ChatBoxBubbleBehavior __instance)
    {
        __instance.enabled = false;
        return false;
    }

    // Player is only assigned in Start, so the OnDestroy would explode.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatBoxBubbleBehavior), nameof(ChatBoxBubbleBehavior.OnDestroy))]
    private static bool Prefix_ChatBoxBubbleBehavior_OnDestroy() => false;
}

internal static class ViewManager_Patches
{
    // public void NotifyUser(string category, string title, float time)
    // NotifyUser("Profile", "Successfully updated your bio!", 5f);
    // NotifyUser("Profile", "Successfully updated your pronouns!", 5f);
    // NotifyUser("Profile", "Successfully uploaded profile pic!", 5f);
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.NotifyUser))]
    private static void Postfix_ViewManager_NotifyUser(string category, string title, float time)
    {
        if (category != "Profile") return;
        if (title is not "Successfully updated your pronouns!"
            and not "Successfully uploaded profile pic!") return;

        PlateManager.RefreshProfile(PlayerSetup.Instance);
        CleanPlatesNetwork.SendProfileUpdate();
    }

    // Hooking this instead of AuthManager.Authenticated for compat with
    // AccountSwitcher mod (doesn't fire the event as it breaks expectations elsewhere).
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.UserLoggedIn))]
    private static void Postfix_ViewManager_UserLoggedIn()
        => PlateManager.OnAuthenticated();
}