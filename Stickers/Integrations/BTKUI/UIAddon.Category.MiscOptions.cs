using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.Stickers.Utilities;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    private static void Setup_OtherOptionsCategory()
    {
        Category debugCategory = _rootPage.AddMelonCategory(ModSettings.Hidden_Foldout_MiscCategory);
        debugCategory.AddMelonToggle(ModSettings.Debug_NetworkInbound);
        debugCategory.AddMelonToggle(ModSettings.Debug_NetworkOutbound);
        debugCategory.AddMelonToggle(ModSettings.Entry_FriendsOnly);
        debugCategory.AddButton("Clear Thumbnail Cache", "Stickers-rubbish-bin", "Clear the cache of all loaded stickers.", ButtonStyle.TextWithIcon)
            .OnPress += () => QuickMenuAPI.ShowConfirm("Clear Thumbnail Cache", "Are you sure you want to clear the Cohtml thumbnail cache for all stickers?", 
            () =>
            {
                StickerCache.ClearCache();
                QuickMenuAPI.ShowAlertToast("Thumbnail cache cleared.", 2);
            });
    }
}