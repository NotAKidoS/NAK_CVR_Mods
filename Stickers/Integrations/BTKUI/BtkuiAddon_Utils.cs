using System.Reflection;
using System.Security.Cryptography;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using UnityEngine;

namespace NAK.Stickers.Integrations;

public static partial class BtkUiAddon
{
    #region Melon Preference Helpers
    
    private static ToggleButton AddMelonToggle(ref Category category, MelonPreferences_Entry<bool> entry)
    {
        ToggleButton toggle = category.AddToggle(entry.DisplayName, entry.Description, entry.Value);
        toggle.OnValueUpdated += b => entry.Value = b;
        return toggle;
    }

    private static SliderFloat AddMelonSlider(ref Category category, MelonPreferences_Entry<float> entry, float min,
        float max, int decimalPlaces = 2, bool allowReset = true)
    {
        SliderFloat slider = category.AddSlider(entry.DisplayName, entry.Description,
            Mathf.Clamp(entry.Value, min, max), min, max, decimalPlaces, entry.DefaultValue, allowReset);
        slider.OnValueUpdated += f => entry.Value = f;
        return slider;
    }

    private static Button AddMelonStringInput(ref Category category, MelonPreferences_Entry<string> entry, string buttonIcon = "", ButtonStyle buttonStyle = ButtonStyle.TextOnly)
    {
        Button button = category.AddButton(entry.DisplayName, buttonIcon, entry.Description, buttonStyle);
        button.OnPress += () => QuickMenuAPI.OpenKeyboard(entry.Value, s => entry.Value = s);
        return button;
    }
    
    private static Button AddMelonNumberInput(ref Category category, MelonPreferences_Entry<float> entry, string buttonIcon = "", ButtonStyle buttonStyle = ButtonStyle.TextOnly)
    {
        Button button = category.AddButton(entry.DisplayName, buttonIcon, entry.Description, buttonStyle);
        button.OnPress += () => QuickMenuAPI.OpenNumberInput(entry.DisplayName, entry.Value, f => entry.Value = f);
        return button;
    }
    
    private static Category AddMelonCategory(ref Page page, MelonPreferences_Entry<bool> entry, bool showHeader = true)
    {
        Category category = page.AddCategory(entry.DisplayName, showHeader, true, entry.Value);
        category.OnCollapse += b => entry.Value = b;
        return category;
    }
    
    #endregion Melon Preference Helpers

    #region Icon Utils
    
    private static Stream GetIconStream(string iconName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name;
        return assembly.GetManifestResourceStream($"{assemblyName}.Resources.{iconName}");
    }

    #endregion Icon Utils
    
    public static void PrepareIconFromMemoryStream(string modName, string iconName, MemoryStream destination)
    {
        if (destination == null)
        {
            StickerMod.Logger.Error("Mod " + modName + " attempted to prepare " + iconName + " but the resource stream was null! Yell at the mod author to fix this!");
        }
        else
        {
            modName = UIUtils.GetCleanString(modName);
            string path1 = @"ChilloutVR_Data\StreamingAssets\Cohtml\UIResources\GameUI\mods\BTKUI\images\" + modName + @"\UserImages";
            if (!Directory.Exists(path1)) Directory.CreateDirectory(path1);
            string path2 = path1 + "\\" + iconName + ".png";
            File.WriteAllBytes(path2, destination.ToArray());
        }
    }
    
    private static void DeleteOldIcon(string modName, string iconName)
    {
        string directoryPath = Path.Combine("ChilloutVR_Data", "StreamingAssets", "Cohtml", "UIResources", "GameUI", "mods", "BTKUI", "images", modName, "UserImages");
        string oldIconPath = Path.Combine(directoryPath, $"{iconName}.png");
        if (!File.Exists(oldIconPath)) 
            return;
        
        File.Delete(oldIconPath);
        //StickerMod.Logger.Msg($"Deleted old icon: {oldIconPath}");
    }
    
    private static void DeleteOldIcons(string modName)
    {
        string directoryPath = Path.Combine("ChilloutVR_Data", "StreamingAssets", "Cohtml", "UIResources", "GameUI", "mods", "BTKUI", "images", modName, "UserImages");
        if (!Directory.Exists(directoryPath))
            return;
        
        foreach (string file in Directory.EnumerateFiles(directoryPath, "*.png"))
        {
            File.Delete(file);
            //StickerMod.Logger.Msg($"Deleted old icon: {file}");
        }
    }
}