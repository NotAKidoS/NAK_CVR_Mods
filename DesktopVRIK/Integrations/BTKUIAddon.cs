using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.Melons.DesktopVRIK;

public static class BTKUIAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        //Add myself to the Misc Menu

        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category miscCategory = miscPage.AddCategory(DesktopVRIKMod.SettingsCategory);

        AddMelonToggle(ref miscCategory, DesktopVRIKMod.EntryEnabled);

        //Add my own page to not clog up Misc Menu
        Page desktopVRIKPage = miscCategory.AddPage("DesktopVRIK Settings", "", "Configure the settings for DesktopVRIK.", "DesktopVRIK");
        desktopVRIKPage.MenuTitle = "DesktopVRIK Settings";
        Category desktopVRIKCategory = desktopVRIKPage.AddCategory(DesktopVRIKMod.SettingsCategory);
        
        // General Settings
        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.EntryPlantFeet);
        
        // Calibration Settings
        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.EntryUseVRIKToes);
        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.EntryFindUnmappedToes);
        
        // Fine-tuning Settings
        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.EntryResetFootstepsOnIdle);
        
        // Body Leaning Weight
        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.EntryBodyLeanWeight, 0, 1f, 1);

        // Max Root Heading Limit & Weights
        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.EntryBodyHeadingLimit, 0, 90f, 0);
        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.EntryPelvisHeadingWeight, 0, 1f, 1);
        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.EntryChestHeadingWeight, 0, 1f, 1);

        // Lerp Speed
        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.EntryIKLerpSpeed, 0, 20f, 0);
    }

    private static void AddMelonToggle(ref Category category, MelonLoader.MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddMelonSlider(ref Page page, MelonLoader.MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
    }
}