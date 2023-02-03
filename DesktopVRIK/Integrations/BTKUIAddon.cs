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

        AddMelonToggle(ref miscCategory, DesktopVRIKMod.m_entryEnabled);

        //Add my own page to not clog up Misc Menu

        Page desktopVRIKPage = miscCategory.AddPage("DesktopVRIK Settings", "", "Configure the settings for DesktopVRIK.", "DesktopVRIK");
        desktopVRIKPage.MenuTitle = "DesktopVRIK Settings";
        desktopVRIKPage.MenuSubtitle = "Simplified settings for VRIK on Desktop.";

        Category desktopVRIKCategory = desktopVRIKPage.AddCategory("DesktopVRIK");

        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.m_entryEnabled);

        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.m_entryEnforceViewPosition);

        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.m_entryEmoteVRIK);

        AddMelonToggle(ref desktopVRIKCategory, DesktopVRIKMod.m_entryEmoteLookAtIK);

        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.m_entryBodyLeanWeight, 0, 1f);

        AddMelonSlider(ref desktopVRIKPage, DesktopVRIKMod.m_entryBodyAngleLimit, 0, 90f);
    }
    private static void AddMelonToggle(ref Category category, MelonLoader.MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddMelonSlider(ref Page page, MelonLoader.MelonPreferences_Entry<float> entry, float min, float max)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max).OnValueUpdated += f => entry.Value = f;
    }
}