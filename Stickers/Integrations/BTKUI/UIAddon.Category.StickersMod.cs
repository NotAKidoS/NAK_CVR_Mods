using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    private static Category _ourCategory;

    private static readonly MultiSelection _sfxSelection = 
        BTKUILibExtensions.CreateMelonMultiSelection(ModSettings.Entry_SelectedSFX);

    private static readonly MultiSelection _desktopKeybindSelection = 
        BTKUILibExtensions.CreateMelonMultiSelection(ModSettings.Entry_PlaceBinding);
    
    #region Category Setup
    
    private static void Setup_StickersModCategory()
    {
        _ourCategory = _rootPage.AddMelonCategory(ModSettings.Hidden_Foldout_SettingsCategory);

        Button placeStickersButton = _ourCategory.AddButton("Place Stickers", "Stickers-magic-wand", "Place stickers via raycast.", ButtonStyle.TextWithIcon);
        placeStickersButton.OnPress += OnPlaceStickersButtonClick;
        
        Button clearSelfStickersButton = _ourCategory.AddButton("Clear Self", "Stickers-eraser", "Clear own stickers.", ButtonStyle.TextWithIcon);
        clearSelfStickersButton.OnPress += OnClearSelfStickersButtonClick;
        
        Button clearAllStickersButton = _ourCategory.AddButton("Clear All", "Stickers-rubbish-bin", "Clear all stickers.", ButtonStyle.TextWithIcon);
        clearAllStickersButton.OnPress += OnClearAllStickersButtonClick;
        
        Button openStickersFolderButton = _ourCategory.AddButton("Open Stickers Folder", "Stickers-folder", "Open UserData/Stickers folder in explorer. If above 256kb your image will automatically be downscaled for networking reasons.", ButtonStyle.TextWithIcon);
        openStickersFolderButton.OnPress += OnOpenStickersFolderButtonClick;
        
        Button openMultiSelectionButton = _ourCategory.AddButton("Sticker SFX", "Stickers-headset", "Choose the SFX used when a sticker is placed.", ButtonStyle.TextWithIcon);
        openMultiSelectionButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_sfxSelection);

        _ourCategory.AddMelonToggle(ModSettings.Entry_HapticsOnPlace);
        
        ToggleButton toggleDesktopKeybindButton = _ourCategory.AddToggle("Use Desktop Keybind", "Should the Desktop keybind be active.", ModSettings.Entry_UsePlaceBinding.Value);
        Button openDesktopKeybindButton = _ourCategory.AddButton("Desktop Keybind", "Stickers-alphabet", "Choose the key binding to place stickers.", ButtonStyle.TextWithIcon);
        openDesktopKeybindButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_desktopKeybindSelection);
        toggleDesktopKeybindButton.OnValueUpdated += (b) =>
        {
            ModSettings.Entry_UsePlaceBinding.Value = b;
            openDesktopKeybindButton.Disabled = !b;
        };

        //AddMelonToggle(ref _ourCategory, ModSettings.Entry_UsePlaceBinding);
    }
    
    #endregion Category Setup

    #region Button Actions
    
    private static void OnPlaceStickersButtonClick()
    {
        if (!_isOurTabOpened) return; 
        string mode = StickerSystem.Instance.IsInStickerMode ? "Exiting" : "Entering";
        QuickMenuAPI.ShowAlertToast($"{mode} sticker placement mode...", 2);
        StickerSystem.Instance.IsInStickerMode = !StickerSystem.Instance.IsInStickerMode;
    }
    
    private static void OnClearSelfStickersButtonClick()
    {
        if (!_isOurTabOpened) return; 
        QuickMenuAPI.ShowAlertToast("Clearing own stickers in world...", 2);
        StickerSystem.Instance.ClearStickersSelf();
    }
    
    private static void OnClearAllStickersButtonClick()
    {
        if (!_isOurTabOpened) return; 
        QuickMenuAPI.ShowAlertToast("Clearing all stickers in world...", 2);
        StickerSystem.Instance.ClearAllStickers();
    }
    
    private static void OnOpenStickersFolderButtonClick()
    {
        if (!_isOurTabOpened) return; 
        QuickMenuAPI.ShowAlertToast("Opening Stickers folder in Explorer...", 2);
        StickerSystem.OpenStickersFolder();
    }
    
    #endregion Button Actions
}