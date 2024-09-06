using BTKUILib.UIObjects;

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static void Setup_DebugOptionsCategory(Page page)
        {
            Category debugCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_DEBUG_SettingsCategory);
            
            AddMelonToggle(ref debugCategory, ModSettings.Debug_NetworkInbound);
            AddMelonToggle(ref debugCategory, ModSettings.Debug_NetworkOutbound);
            AddMelonToggle(ref debugCategory, ModSettings.Debug_ComponentSearchTime);
        }
    }
}