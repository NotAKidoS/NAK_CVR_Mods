using BTKUILib;
using BTKUILib.UIObjects;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;
using System.Reflection;

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

    #region Setup
    
    private static void Setup_Icons()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name;
        
        // All icons used - https://www.flaticon.com/authors/gohsantosadrive
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-alphabet",  GetIconStream("Gohsantosadrive_Icons.Stickers-alphabet.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-eraser", GetIconStream("Gohsantosadrive_Icons.Stickers-eraser.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-folder", GetIconStream("Gohsantosadrive_Icons.Stickers-folder.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-headset", GetIconStream("Gohsantosadrive_Icons.Stickers-headset.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magnifying-glass", GetIconStream("Gohsantosadrive_Icons.Stickers-magnifying-glass.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magic-wand", GetIconStream("Gohsantosadrive_Icons.Stickers-magic-wand.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magic-wand-broken", GetIconStream("Gohsantosadrive_Icons.Stickers-magic-wand-broken.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-mouse", GetIconStream("Gohsantosadrive_Icons.Stickers-mouse.png"));
        //QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-pencil", GetIconStream("Gohsantosadrive_Icons.Stickers-pencil.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-puzzle", GetIconStream("Gohsantosadrive_Icons.Stickers-puzzle.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-puzzle-disabled", GetIconStream("Gohsantosadrive_Icons.Stickers-puzzle-disabled.png")); // disabled Sticker Puzzle
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-rubbish-bin", GetIconStream("Gohsantosadrive_Icons.Stickers-rubbish-bin.png"));
        
        return;
        Stream GetIconStream(string iconName) => assembly.GetManifestResourceStream($"{assemblyName}.Resources.{iconName}");
    }
    
    private static void Setup_StickerModTab()
    {
        _rootPage = new Page(ModSettings.ModName, ModSettings.SM_SettingsCategory, true, "Stickers-Puzzle") // sticker icon will be left blank as it is updated on world join, AFTER Icon value is exposed..
        {
            MenuTitle = ModSettings.SM_SettingsCategory + $" (Network Version v{ModNetwork.NetworkVersion})",
            MenuSubtitle = "", // left this blank as it is defined when the world loads
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
        Setup_OtherOptionsCategory();
    }

    #endregion Setup

    #region Double-Click Place Sticker

    private static DateTime lastTime = DateTime.Now;

    private static void OnTabChange(string newTab, string previousTab)
    {
        _isOurTabOpened = newTab == _rootPageElementID;
        if (!_isOurTabOpened) return;
        
        TimeSpan timeDifference = DateTime.Now - lastTime;
        if (timeDifference.TotalSeconds <= 0.5)
        {
            switch (ModSettings.Entry_TabDoubleClick.Value)
            {
                default:
                case TabDoubleClick.ToggleStickerMode:
                    OnPlaceStickersButtonClick();
                    break;
                case TabDoubleClick.ClearAllStickers:
                    OnClearAllStickersButtonClick();
                    break;
                case TabDoubleClick.ClearSelfStickers:
                    OnClearSelfStickersButtonClick();
                    break;
                case TabDoubleClick.None:
                    break;
            }
            return;
        }
        lastTime = DateTime.Now;
    }

    #endregion Double-Click Place Sticker
}