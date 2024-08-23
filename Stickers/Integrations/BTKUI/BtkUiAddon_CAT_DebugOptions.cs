using BTKUILib.UIObjects;

namespace NAK.Stickers.Integrations
{
    public static partial class BtkUiAddon
    {
        private static void Setup_DebugOptionsCategory(Page page)
        {
            Category debugCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_DebugCategory);
            
            AddMelonToggle(ref debugCategory, ModSettings.Debug_NetworkInbound);
            AddMelonToggle(ref debugCategory, ModSettings.Debug_NetworkOutbound);
        }
    }
}