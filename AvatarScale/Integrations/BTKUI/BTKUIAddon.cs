using System.Runtime.CompilerServices;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using BTKUILib;
using BTKUILib.UIObjects;
using MelonLoader;
using NAK.AvatarScaleMod.Integrations.BTKUI;
using UnityEngine;

namespace NAK.AvatarScaleMod.Integrations;

public static class BTKUIAddon
{
    #region Initialization

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        Page as_RootPage = new(ModSettings.SettingsCategory, ModSettings.SettingsCategory, true, "")
        {
            MenuTitle = ModSettings.SettingsCategory,
            MenuSubtitle = "Avatar Scale Mod Settings"
        };

        QuickMenuAPI.OnMenuRegenerate += (_) => 
        {
            SchedulerSystem.AddJob((InjectMenu), 1f, 1f, 1);
        };
    }

    // private static void InjectMenu()
    // {
    //     MelonLogger.Msg("Injecting into QM!");
    //     CVR_MenuManager.Instance.quickMenu.View.ExecuteScript(Scripts.GetEmbeddedScript("menu.js"));
    // }
    
    private static void InjectMenu()
    {
        MelonLogger.Msg("Injecting into QM!");
        string menuJsPath = Path.Combine(Application.streamingAssetsPath, "Cohtml", "UIResources", "AvatarScaleMod", "menu.js");
        string menuJsContent = ReadJSFile(menuJsPath);
        CVR_MenuManager.Instance.quickMenu.View._view.ExecuteScript(menuJsContent);
    }

    private static string ReadJSFile(string path)
    {
        if(File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        MelonLogger.Warning($"File not found: {path}");
        return string.Empty;
    }

    #endregion

    #region Melon Pref Helpers

    internal static void AddMelonToggle(ref Category category, MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    internal static void AddMelonSlider(ref Page page, MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
    }

    #endregion
}