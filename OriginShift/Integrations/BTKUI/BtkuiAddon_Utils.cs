using System.Reflection;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using UnityEngine;

namespace NAK.OriginShiftMod.Integrations
{
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
        
        private static Category AddMelonCategory(ref Page page, string displayName, bool showHeader = true)
        {
            Category category = page.AddCategory(displayName, showHeader);
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
    }
}