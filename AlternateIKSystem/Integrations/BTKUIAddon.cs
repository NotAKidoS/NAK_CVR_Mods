using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.AlternateIKSystem.Integrations;

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

        SetupGeneralIKConfigurationPage(ref miscCategory);
        SetupDesktopIKConfigurationPage(ref miscCategory);
        SetupHalfBodyIKConfigurationPage(ref miscCategory);
    }

    #endregion

    #region Pages Setup

    private static void SetupGeneralIKConfigurationPage(ref Category parentCategory)
    {
        Page generalIKPage = parentCategory.AddPage("General IK Settings", "", "Configure the settings for general IK.", ModSettings.SettingsCategory);
        generalIKPage.MenuTitle = "General IK Settings";
        Category generalIKCategory = generalIKPage.AddCategory(generalIKPage.MenuTitle);

        // General Settings
        AddMelonToggle(ref generalIKCategory, ModSettings.EntryPlantFeet);

        // Calibration Settings
        AddMelonToggle(ref generalIKCategory, ModSettings.EntryUseToesForVRIK);

        // Fine-tuning Settings
        AddMelonToggle(ref generalIKCategory, ModSettings.EntryResetFootstepsOnIdle);

        // Fake root heading limit
        AddMelonSlider(ref generalIKPage, ModSettings.EntryBodyHeadingLimit, 0, 90f, 0);
        
        // Lerp Speed
        AddMelonSlider(ref generalIKPage, ModSettings.EntryIKLerpSpeed, 0, 20f, 0);
    }

    private static void SetupDesktopIKConfigurationPage(ref Category parentCategory)
    {
        Page desktopIKPage = parentCategory.AddPage("Desktop IK Settings", "", "Configure the settings for desktop IK.", ModSettings.SettingsCategory);
        desktopIKPage.MenuTitle = "Desktop IK Settings";
        Category desktopIKCategory = desktopIKPage.AddCategory(desktopIKPage.MenuTitle);

        // Funny Settings
        AddMelonToggle(ref desktopIKCategory, ModSettings.EntryProneThrusting);

        // Body Leaning Weight
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryBodyLeanWeight, 0, 1f, 1);

        // Max Root Heading Limit & Weights
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryPelvisHeadingWeight, 0, 1f, 1);
        AddMelonSlider(ref desktopIKPage, ModSettings.EntryChestHeadingWeight, 0, 1f, 1);
    }

    private static void SetupHalfBodyIKConfigurationPage(ref Category parentCategory)
    {
        Page halfBodyIKPage = parentCategory.AddPage("HalfBody IK Settings", "", "Configure the settings for halfbody IK.", ModSettings.SettingsCategory);
        halfBodyIKPage.MenuTitle = "HalfBody IK Settings";
        //Category halfBodyIKCategory = halfBodyIKPage.AddCategory(halfBodyIKPage.MenuTitle);
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