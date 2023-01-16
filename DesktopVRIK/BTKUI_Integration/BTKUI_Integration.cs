using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.Melons.DesktopVRIK.BTKUI_Integration;

public static class BTKUI_Integration
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        //Add myself to the Misc Menu

        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category miscCategory = miscPage.AddCategory("DesktopVRIK");

        var setting_Enabled = miscCategory.AddToggle(DesktopVRIKMod.m_entryEnabled.DisplayName, DesktopVRIKMod.m_entryEnabled.Description, DesktopVRIKMod.m_entryEnabled.Value);
        setting_Enabled.OnValueUpdated += b => DesktopVRIKMod.m_entryEnabled.Value = b;

        //Add my own page to not clog up Misc Menu

        Page desktopVRIKPage = miscCategory.AddPage("DesktopVRIK Settings", "", "Configure the settings for DesktopVRIK.", "DesktopVRIK");
        desktopVRIKPage.MenuTitle = "DesktopVRIK Settings";
        desktopVRIKPage.MenuSubtitle = "Simplified settings for VRIK on Desktop.";

        Category desktopVRIKCategory = desktopVRIKPage.AddCategory("DesktopVRIK");

        var setting_Enabled_2 = desktopVRIKCategory.AddToggle(DesktopVRIKMod.m_entryEnabled.DisplayName, DesktopVRIKMod.m_entryEnabled.Description, DesktopVRIKMod.m_entryEnabled.Value);
        setting_Enabled_2.OnValueUpdated += b => DesktopVRIKMod.m_entryEnabled.Value = b;

        var setting_EnforceViewPosition = desktopVRIKCategory.AddToggle(DesktopVRIKMod.m_entryEnforceViewPosition.DisplayName, DesktopVRIKMod.m_entryEnforceViewPosition.Description, DesktopVRIKMod.m_entryEnforceViewPosition.Value);
        setting_EnforceViewPosition.OnValueUpdated += b => DesktopVRIKMod.m_entryEnforceViewPosition.Value = b;

        var setting_DisableEmoteVRIK = desktopVRIKCategory.AddToggle(DesktopVRIKMod.m_entryEmoteVRIK.DisplayName, DesktopVRIKMod.m_entryEmoteVRIK.Description, DesktopVRIKMod.m_entryEmoteVRIK.Value);
        setting_DisableEmoteVRIK.OnValueUpdated += b => DesktopVRIKMod.m_entryEmoteVRIK.Value = b;

        var setting_DisableEmoteLookAtIK = desktopVRIKCategory.AddToggle(DesktopVRIKMod.m_entryEmoteLookAtIK.DisplayName, DesktopVRIKMod.m_entryEmoteLookAtIK.Description, DesktopVRIKMod.m_entryEmoteLookAtIK.Value);
        setting_DisableEmoteLookAtIK.OnValueUpdated += b => DesktopVRIKMod.m_entryEmoteLookAtIK.Value = b;

        var setting_BodyLeanWeight = desktopVRIKPage.AddSlider(DesktopVRIKMod.m_entryBodyLeanWeight.DisplayName, DesktopVRIKMod.m_entryBodyLeanWeight.Description, DesktopVRIKMod.m_entryBodyLeanWeight.Value, 0f, 1f, 1);
        setting_BodyLeanWeight.OnValueUpdated += f => DesktopVRIKMod.m_entryBodyLeanWeight.Value = f;

        var setting_BodyAngleLimit = desktopVRIKPage.AddSlider(DesktopVRIKMod.m_entryBodyAngleLimit.DisplayName, DesktopVRIKMod.m_entryBodyAngleLimit.Description, DesktopVRIKMod.m_entryBodyAngleLimit.Value, 0f, 90f, 0);
        setting_BodyAngleLimit.OnValueUpdated += f => DesktopVRIKMod.m_entryBodyAngleLimit.Value = f;
    }
}