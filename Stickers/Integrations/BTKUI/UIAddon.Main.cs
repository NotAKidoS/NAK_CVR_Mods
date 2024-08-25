using BTKUILib;
using BTKUILib.UIObjects;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    private static Page _rootPage;
    private static string _rootPageElementID;
    
    private static bool _isOurTabOpened;

    public static void Initialize()
    {
        Setup_Icons();
        Setup_StickerModTab();
        Setup_PlayerOptionsPage();
    }

    #region Initialization

    private static void Setup_Icons()
    {
        // All icons used - https://www.flaticon.com/authors/gohsantosadrive
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-alphabet", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-alphabet.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-eraser", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-eraser.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-folder", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-folder.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-headset", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-headset.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magnifying-glass", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-magnifying-glass.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magic-wand", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-magic-wand.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-pencil", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-pencil.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-puzzle", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-puzzle.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-rubbish-bin", BTKUILibExtensions.GetIconStream("Gohsantosadrive_Icons.Stickers-rubbish-bin.png"));
    }

    private static void Setup_StickerModTab()
    {
        _rootPage = new Page(ModSettings.ModName, ModSettings.SM_SettingsCategory, true, "Stickers-puzzle")
        {
            MenuTitle = ModSettings.SM_SettingsCategory,
            MenuSubtitle = "Stickers! Double-click the tab to quickly toggle Sticker Mode.",
        };

        _rootPageElementID = _rootPage.ElementID;
        
        QuickMenuAPI.OnTabChange += OnTabChange;
        ModNetwork.OnTextureOutboundStateChanged += (isSending) =>
        {
            if (_isOurTabOpened && isSending) QuickMenuAPI.ShowAlertToast("Sending Sticker over Mod Network...", 2);
            //_rootPage.Disabled = isSending; // TODO: fix being able to select stickers while sending
        };
        
        StickerSystem.OnStickerLoaded += (slotIndex, imageRelativePath) =>
        {
            if (_isOurTabOpened) QuickMenuAPI.ShowAlertToast($"Sticker loaded: {imageRelativePath}", 2);
            _stickerSelectionButtons[slotIndex].ButtonIcon = StickerCache.GetBtkUiIconName(imageRelativePath);
        };
        
        StickerSystem.OnStickerLoadFailed += (slotIndex, error) =>
        {
            if (_isOurTabOpened) QuickMenuAPI.ShowAlertToast(error, 3);
        };

        Setup_StickersModCategory();
        Setup_StickerSelectionCategory();
        Setup_DebugOptionsCategory();
    }

    #endregion

    #region Double-Click Place Sticker

    private static DateTime lastTime = DateTime.Now;

    private static void OnTabChange(string newTab, string previousTab)
    {
        _isOurTabOpened = newTab == _rootPageElementID;
        if (!_isOurTabOpened) return;
        
        TimeSpan timeDifference = DateTime.Now - lastTime;
        if (timeDifference.TotalSeconds <= 0.5)
        {
            StickerSystem.Instance.IsInStickerMode = !StickerSystem.Instance.IsInStickerMode;
            return;
        }
        lastTime = DateTime.Now;
    }

    #endregion Double-Click Place Sticker
}