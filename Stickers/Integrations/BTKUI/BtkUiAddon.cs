using ABI_RC.Core.Player;
using BTKUILib;
using BTKUILib.UIObjects;

namespace NAK.Stickers.Integrations;

public static partial class BtkUiAddon
{
    private static Page _rootPage;
    private static string _rootPageElementID;
    
    private static bool _isOurTabOpened;
    private static Action _onOurTabOpened;

    public static void Initialize()
    {
        Prepare_Icons();
        Setup_AvatarScaleModTab();
        //Setup_PlayerSelectPage();
    }

    #region Initialization

    private static void Prepare_Icons()
    {
        // All icons used - https://www.flaticon.com/authors/gohsantosadrive
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-alphabet", GetIconStream("Gohsantosadrive_Icons.Stickers-alphabet.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-eraser", GetIconStream("Gohsantosadrive_Icons.Stickers-eraser.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-folder", GetIconStream("Gohsantosadrive_Icons.Stickers-folder.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-headset", GetIconStream("Gohsantosadrive_Icons.Stickers-headset.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-magic-wand", GetIconStream("Gohsantosadrive_Icons.Stickers-magic-wand.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-pencil", GetIconStream("Gohsantosadrive_Icons.Stickers-pencil.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-puzzle", GetIconStream("Gohsantosadrive_Icons.Stickers-puzzle.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, "Stickers-rubbish-bin", GetIconStream("Gohsantosadrive_Icons.Stickers-rubbish-bin.png"));
    }

    private static void Setup_AvatarScaleModTab()
    {
        _rootPage = new Page(ModSettings.ModName, ModSettings.SM_SettingsCategory, true, "Stickers-puzzle")
        {
            MenuTitle = ModSettings.SM_SettingsCategory,
            MenuSubtitle = "Stickers! Double-click the tab to quickly toggle Sticker Mode.",
        };
        
        _rootPageElementID = _rootPage.ElementID;
        QuickMenuAPI.OnTabChange += OnTabChange;
        // QuickMenuAPI.UserJoin += OnUserJoinLeave;
        // QuickMenuAPI.UserLeave += OnUserJoinLeave;
        // QuickMenuAPI.OnWorldLeave += OnWorldLeave;
        
        Setup_StickersModCategory(_rootPage);
        Setup_StickerSelectionCategory(_rootPage);
        Setup_DebugOptionsCategory(_rootPage);
    }

    #endregion

    #region Double-Click Place Sticker

    private static DateTime lastTime = DateTime.Now;

    private static void OnTabChange(string newTab, string previousTab)
    {
        _isOurTabOpened = newTab == _rootPageElementID;
        if (_isOurTabOpened)
        {
            _onOurTabOpened?.Invoke();
            TimeSpan timeDifference = DateTime.Now - lastTime;
            if (timeDifference.TotalSeconds <= 0.5)
            {
                //AvatarScaleManager.Instance.Setting_UniversalScaling = false;
                StickerSystem.Instance.IsInStickerMode = !StickerSystem.Instance.IsInStickerMode;
                return;
            }
        }
        lastTime = DateTime.Now;
    }

    #endregion Double-Click Place Sticker
}
