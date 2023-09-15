using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.DesktopVRSwitch.Integrations;

public static class BTKUIAddon
{
    #region Initialization

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        // Add mod to the Misc Menu
        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category vrSwitchMiscCategory = miscPage.AddCategory(ModSettings.SettingsCategory);

        vrSwitchMiscCategory.AddButton("Switch VR Mode", "", "Switch to Desktop/VR.").OnPress +=
            () =>
            {
                QuickMenuAPI.ShowConfirm(
                    title: "Switch VR Mode",
                    content: "Are you sure you want to switch to Desktop/VR?",
                    onYes: () => VRModeSwitchManager.Instance.AttemptSwitch()
                );
            };
        
        SetupSwitchConfigurationPage(ref vrSwitchMiscCategory);
    }

    #endregion

    #region Pages Setup

    private static void SetupSwitchConfigurationPage(ref Category parentCategory)
    {
        Page vrSwitchPage = parentCategory.AddPage("DesktopVRSwitch Settings", "", "Configure the settings for DesktopVRSwitch.", ModSettings.SettingsCategory);
        vrSwitchPage.MenuTitle = "DesktopVRSwitch Settings";
        Category vrSwitchCategory = vrSwitchPage.AddCategory(vrSwitchPage.MenuTitle);

        AddMelonToggle(ref vrSwitchCategory, ModSettings.EntryEnterCalibrationOnSwitch);
        
        AddMelonToggle(ref vrSwitchCategory, ModSettings.EntryUseTransitionOnSwitch);
        
        AddMelonToggle(ref vrSwitchCategory, ModSettings.EntrySwitchToDesktopOnExit);
    }

    #endregion

    #region Melon Pref Helpers

    private static void AddMelonToggle(ref Category category, MelonLoader.MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    // private static void AddMelonSlider(ref Page page, MelonLoader.MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    // {
    //     page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
    // }

    #endregion
}