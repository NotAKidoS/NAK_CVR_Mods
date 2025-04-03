using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.DesktopVRIK.Integrations;

public static class BTKUIAddon
{
    #region Initialization

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        // Add mod to the Misc Menu
        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category miscCategory = miscPage.AddCategory(ModSettings.SettingsCategory);

        AddMelonToggle(ref miscCategory, ModSettings.EntryEnabled);

        SetupDesktopIKConfigurationPage(ref miscCategory);
    }

    #endregion

    #region Pages Setup

    private static void SetupDesktopIKConfigurationPage(ref Category parentCategory)
    {
        Page desktopIKPage = parentCategory.AddPage("DesktopVRIK Settings", "", "Configure the settings for DesktopVRIK.", ModSettings.SettingsCategory);
        desktopIKPage.MenuTitle = "DesktopVRIK Settings";
        Category desktopIKCategory = desktopIKPage.AddCategory(desktopIKPage.MenuTitle);

        // General Settings
        AddMelonToggle(ref desktopIKCategory, ModSettings.EntryPlantFeet);

        // Calibration Settings
        AddMelonToggle(ref desktopIKCategory, ModSettings.EntryUseToesForVRIK);

        // Fine-tuning Settings
        AddMelonToggle(ref desktopIKCategory, ModSettings.EntryResetFootstepsOnIdle);

        // Funny Settings
        AddMelonToggle(ref desktopIKCategory, ModSettings.EntryProneThrusting);

        // Body Leaning Weight
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryBodyLeanWeight, 0, 1f, 1);

        // Max Root Heading Limit & Weights
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryBodyHeadingLimit, 0, 90f, 0);
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryPelvisHeadingWeight, 0, 1f, 1);
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryChestHeadingWeight, 0, 1f, 1);

        // Lerp Speed
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryIKLerpSpeed, 0, 20f, 0);
    }

    #endregion

    #region Melon Pref Helpers

    private static void AddMelonToggle(ref Category category, MelonLoader.MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddMelonSlider(ref Page page, MelonLoader.MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
    }

    #endregion
}