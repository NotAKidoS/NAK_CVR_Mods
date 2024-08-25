using BTKUILib.UIObjects;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    private static void Setup_DebugOptionsCategory()
    {
        Category debugCategory = _rootPage.AddMelonCategory(ModSettings.Hidden_Foldout_DebugCategory);
        debugCategory.AddMelonToggle(ModSettings.Debug_NetworkInbound);
        debugCategory.AddMelonToggle(ModSettings.Debug_NetworkOutbound);
    }
}