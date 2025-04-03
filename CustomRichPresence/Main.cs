using System.Reflection;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Helpers;
using HarmonyLib;
using MagicaCloth;
using MelonLoader;
using Steamworks;
using UnityEngine;

namespace NAK.CustomRichPresence;

public class CustomRichPresenceMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(CustomRichPresence));

    private static readonly MelonPreferences_Entry<bool> EntryUseCustomPresence =
        Category.CreateEntry("use_custom_presence", true,
            "Use Custom Presence", description: "Uses the custom rich presence setup.");

    // Discord Rich Presence Customization
    private static readonly MelonPreferences_Entry<string> DiscordStatusFormat =
        Category.CreateEntry("discord_status_format", "Online using {mode}.",
            "Discord Status Format", description: "Format for Discord status message. Available variables: {mode}, {instance_name}, {privacy}");
    
    private static readonly MelonPreferences_Entry<string> DiscordDetailsFormat =
        Category.CreateEntry("discord_details_format", "{instance_name} [{privacy}]",
            "Discord Details Format", description: "Format for Discord details. Available variables: {instance_name}, {privacy}, {world_name}, {mission_name}");

    // Steam Rich Presence Customization
    private static readonly MelonPreferences_Entry<string> SteamStatusFormat =
        Category.CreateEntry("steam_status_format", "Exploring ({mode}) {instance_name} [{privacy}].",
            "Steam Status Format", description: "Format for Steam status. Available variables: {mode}, {instance_name}, {privacy}");

    private static readonly MelonPreferences_Entry<string> SteamGameStatusFormat =
        Category.CreateEntry("steam_game_status_format", "Connected to a server, Privacy: {privacy}.",
            "Steam Game Status Format", description: "Format for Steam game status. Available variables: {privacy}, {instance_name}, {world_name}");

    private static readonly MelonPreferences_Entry<string> SteamDisplayStatus =
        Category.CreateEntry("steam_display_status", "#Status_Online",
            "Steam Display Status", description: "Steam display status localization key.");

    #endregion Melon Preferences
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(RichPresence).GetMethod(nameof(RichPresence.PopulateLastMessage),
                BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(CustomRichPresenceMod).GetMethod(nameof(OnPopulateLastMessage),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static string FormatPresenceText(string format, string mode)
    {
        return format
            .Replace("{mode}", mode)
            .Replace("{instance_name}", RichPresence.LastMsg.InstanceName)
            .Replace("{privacy}", RichPresence.LastMsg.InstancePrivacy)
            .Replace("{world_name}", RichPresence.LastMsg.InstanceWorldName)
            .Replace("{mission_name}", RichPresence.LastMsg.InstanceMissionName)
            .Replace("{current_players}", RichPresence.LastMsg.CurrentPlayers.ToString())
            .Replace("{max_players}", RichPresence.LastMsg.MaxPlayers.ToString());
    }

    private static bool OnPopulateLastMessage()
    {
        if (!EntryUseCustomPresence.Value)
            return true;

        string mode = MetaPort.Instance.isUsingVr ? "VR Mode" : "Desktop Mode";
        
        PresenceManager.ClearPresence();
        if (RichPresence.DiscordEnabled)
        {
            string status = FormatPresenceText(DiscordStatusFormat.Value, mode);
            string details = FormatPresenceText(DiscordDetailsFormat.Value, mode);
            
            PresenceManager.UpdatePresence(
                status,
                details,
                RichPresence.LastConnectedToServer,
                0L,
                "discordrp-cvrmain",
                null,
                null,
                null,
                RichPresence.LastMsg.InstanceMeshId,
                RichPresence.LastMsg.CurrentPlayers,
                RichPresence.LastMsg.MaxPlayers
            );
        }

        if (!CheckVR.Instance.skipSteamApiRegister && SteamManager.Initialized)
        {
            SteamFriends.ClearRichPresence();
            if (RichPresence.SteamEnabled)
            {
                string status = FormatPresenceText(SteamStatusFormat.Value, mode);
                string gameStatus = FormatPresenceText(SteamGameStatusFormat.Value, mode);

                SteamFriends.SetRichPresence("status", status);
                SteamFriends.SetRichPresence("gamestatus", gameStatus);
                SteamFriends.SetRichPresence("steam_display", SteamDisplayStatus.Value);
                SteamFriends.SetRichPresence("steam_player_group", RichPresence.LastMsg.InstanceMeshId);
                SteamFriends.SetRichPresence("steam_player_group_size", RichPresence.LastMsg.CurrentPlayers.ToString());
                SteamFriends.SetRichPresence("gamemode", RichPresence.LastMsg.InstanceMissionName);
                SteamFriends.SetRichPresence("worldname", RichPresence.LastMsg.InstanceWorldName);
                SteamFriends.SetRichPresence("instancename", RichPresence.LastMsg.InstanceName);
                SteamFriends.SetRichPresence("instanceprivacy", RichPresence.LastMsg.InstancePrivacy);
                SteamFriends.SetRichPresence("currentplayer", RichPresence.LastMsg.CurrentPlayers.ToString());
                SteamFriends.SetRichPresence("maxplayer", RichPresence.LastMsg.MaxPlayers.ToString());
            }
        }

        return false;
    }
}