using System.Reflection;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using JetBrains.Annotations;
using MelonLoader;
using UnityEngine;

namespace NAK.Stickers.Integrations;

[PublicAPI]
public static class BTKUILibExtensions
{
    #region Icon Utils

    public static Stream GetIconStream(string iconName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name;
        return assembly.GetManifestResourceStream($"{assemblyName}.Resources.{iconName}");
    }

    #endregion Icon Utils
    
    #region Enum Utils

    private static class EnumUtils
    {
        public static string[] GetPrettyEnumNames<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).Select(PrettyFormatEnumName).ToArray();
        }

        private static string PrettyFormatEnumName(string name)
        {
            // adds spaces before capital letters (excluding the first letter)
            return System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");
        }

        public static int GetEnumIndex<T>(T value) where T : Enum
        {
            return Array.IndexOf(Enum.GetValues(typeof(T)), value);
        }
    }

    #endregion Enum Utils
    
    #region Melon Preference Extensions
    
    public static ToggleButton AddMelonToggle(this Category category, MelonPreferences_Entry<bool> entry)
    {
        ToggleButton toggle = category.AddToggle(entry.DisplayName, entry.Description, entry.Value);
        toggle.OnValueUpdated += b => entry.Value = b;
        return toggle;
    }

    public static SliderFloat AddMelonSlider(this Category category, MelonPreferences_Entry<float> entry, float min,
        float max, int decimalPlaces = 2, bool allowReset = true)
    {
        SliderFloat slider = category.AddSlider(entry.DisplayName, entry.Description,
            Mathf.Clamp(entry.Value, min, max), min, max, decimalPlaces, entry.DefaultValue, allowReset);
        slider.OnValueUpdated += f => entry.Value = f;
        return slider;
    }

    public static Button AddMelonStringInput(this Category category, MelonPreferences_Entry<string> entry, string buttonIcon = "", ButtonStyle buttonStyle = ButtonStyle.TextOnly)
    {
        Button button = category.AddButton(entry.DisplayName, buttonIcon, entry.Description, buttonStyle);
        button.OnPress += () => QuickMenuAPI.OpenKeyboard(entry.Value, s => entry.Value = s);
        return button;
    }
    
    public static Button AddMelonNumberInput(this Category category, MelonPreferences_Entry<float> entry, string buttonIcon = "", ButtonStyle buttonStyle = ButtonStyle.TextOnly)
    {
        Button button = category.AddButton(entry.DisplayName, buttonIcon, entry.Description, buttonStyle);
        button.OnPress += () => QuickMenuAPI.OpenNumberInput(entry.DisplayName, entry.Value, f => entry.Value = f);
        return button;
    }
    
    public static Category AddMelonCategory(this Page page, MelonPreferences_Entry<bool> entry, bool showHeader = true)
    {
        Category category = page.AddCategory(entry.DisplayName, showHeader, true, !entry.Value);
        category.OnCollapse += b => entry.Value = !b; // more intuitive if pref value of true means category open
        return category;
    }
    
    public static MultiSelection CreateMelonMultiSelection<TEnum>(MelonPreferences_Entry<TEnum> entry) where TEnum : Enum
    {
        MultiSelection multiSelection = new(
            entry.DisplayName,
            EnumUtils.GetPrettyEnumNames<TEnum>(),
            EnumUtils.GetEnumIndex(entry.Value)
        )
        {
            OnOptionUpdated = i => entry.Value = (TEnum)Enum.Parse(typeof(TEnum), Enum.GetNames(typeof(TEnum))[i])
        };

        return multiSelection;
    }
    
    #endregion Melon Preference Extensions
}