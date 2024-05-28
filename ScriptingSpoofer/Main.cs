using System.Reflection;
using ABI_RC.API;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Systems.GameEventSystem;
using HarmonyLib;
using MelonLoader;
using Random = UnityEngine.Random;

namespace NAK.ScriptingSpoofer;

public class ScriptingSpoofer : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ScriptingSpoofer));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle scripting spoofer.");
    
    public static readonly MelonPreferences_Entry<bool> EntryCensorUsername =
        Category.CreateEntry("Censor Username", true, description: "Censor username. Toggle to randomize username instead.");

    private static string spoofedUsername;
    private static string spoofedUserId;
    
    private static readonly char[] CensorChars = {'!', '@', '#', '$', '%', '^', '&', '*'};

    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(PlayerApiPatches));
        
        // Regenerate spoofed data on login
        CVRGameEventSystem.Authentication.OnLogin.AddListener(GenerateRandomSpoofedData);
    }
    
    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }

    private static void GenerateRandomSpoofedData(UserAuthResponse _) // we get manually
    {
        spoofedUsername = EntryCensorUsername.Value ? GenerateCensoredUsername() : GenerateRandomUsername();
        spoofedUserId = Guid.NewGuid().ToString();
    }
    
    private static string GenerateCensoredUsername()
    {
        var originalUsername = AuthManager.Username;
        var usernameArray = originalUsername.ToCharArray();

        for (var i = 0; i < usernameArray.Length; i++)
            if (Random.value > 0.7f) usernameArray[i] = CensorChars[Random.Range(0, CensorChars.Length)];

        var modifiedUsername = new string(usernameArray);
        string[] prefixes = { "xX", "_", Random.Range(10, 99).ToString() };
        string[] suffixes = { "Xx", "_", Random.Range(10, 99).ToString() };

        var prefix = prefixes[Random.Range(0, prefixes.Length)];
        var suffix = suffixes[Random.Range(0, suffixes.Length)];

        return prefix + modifiedUsername + suffix;
    }
    
    private static string GenerateRandomUsername()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] username = new char[Random.Range(5, 15)];
        for (int i = 0; i < username.Length; i++)
            username[i] = chars[Random.Range(0, chars.Length)];

        return new string(username);
    }

    private static class PlayerApiPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalPlayerApi), nameof(LocalPlayerApi.Username), MethodType.Getter)]
        [HarmonyPatch(typeof(PlayerApiBase), nameof(PlayerApiBase.Username), MethodType.Getter)]
        private static bool GetSpoofedUsername(ref PlayerApiBase __instance, ref string __result)
        {
            if (__instance.IsRemote) return true;
            if (!EntryEnabled.Value) return true;

            __result = spoofedUsername;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalPlayerApi), nameof(LocalPlayerApi.UserID), MethodType.Getter)]
        [HarmonyPatch(typeof(PlayerApiBase), nameof(PlayerApiBase.UserID), MethodType.Getter)]
        private static bool GetSpoofedUserId(ref PlayerApiBase __instance, ref string __result)
        {
            if (__instance.IsRemote) return true;
            if (!EntryEnabled.Value) return true;

            __result = spoofedUserId;
            return false;
        }
    }
}