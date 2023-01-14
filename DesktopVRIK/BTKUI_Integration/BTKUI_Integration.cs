using System.Runtime.CompilerServices;
using BTKUILib;
using BTKUILib.UIObjects;

namespace NAK.Melons.DesktopVRIK.BTKUI_Integration;

public static class BTKUI_Integration
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category CategoryUI = miscPage.AddCategory("DesktopVRIK");

        var setting_Enabled = CategoryUI.AddToggle(DesktopVRIKMod.m_entryEnabled.DisplayName, DesktopVRIKMod.m_entryEnabled.Description, DesktopVRIKMod.m_entryEnabled.Value);
        setting_Enabled.OnValueUpdated += b => DesktopVRIKMod.m_entryEnabled.Value = b;

        var setting_EnforceViewPosition = CategoryUI.AddToggle(DesktopVRIKMod.m_entryEnforceViewPosition.DisplayName, DesktopVRIKMod.m_entryEnforceViewPosition.Description, DesktopVRIKMod.m_entryEnforceViewPosition.Value);
        setting_EnforceViewPosition.OnValueUpdated += b => DesktopVRIKMod.m_entryEnforceViewPosition.Value = b;

        var setting_DisableEmoteVRIK = CategoryUI.AddToggle(DesktopVRIKMod.m_entryEmoteVRIK.DisplayName, DesktopVRIKMod.m_entryEmoteVRIK.Description, DesktopVRIKMod.m_entryEmoteVRIK.Value);
        setting_DisableEmoteVRIK.OnValueUpdated += b => DesktopVRIKMod.m_entryEmoteVRIK.Value = b;

        var setting_DisableEmoteLookAtIK = CategoryUI.AddToggle(DesktopVRIKMod.m_entryEmoteLookAtIK.DisplayName, DesktopVRIKMod.m_entryEmoteLookAtIK.Description, DesktopVRIKMod.m_entryEmoteLookAtIK.Value);
        setting_DisableEmoteLookAtIK.OnValueUpdated += b => DesktopVRIKMod.m_entryEmoteLookAtIK.Value = b;

        var setting_EmulateHipMovementWeight = miscPage.AddSlider(DesktopVRIKMod.m_entryEmulateVRChatHipMovementWeight.DisplayName, DesktopVRIKMod.m_entryEmulateVRChatHipMovementWeight.Description, DesktopVRIKMod.m_entryEmulateVRChatHipMovementWeight.Value, 0f, 1f);
        setting_EmulateHipMovementWeight.OnValueUpdated += f => DesktopVRIKMod.m_entryEmulateVRChatHipMovementWeight.Value = f;
    }
}