using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    private static Category _ourCategory;

    private static readonly MultiSelection _sfxSelection = 
        MultiSelection.CreateMultiSelectionFromMelonPref(ModSettings.Entry_SelectedSFX);

    private static readonly MultiSelection _desktopKeybindSelection = 
        MultiSelection.CreateMultiSelectionFromMelonPref(ModSettings.Entry_PlaceBinding);
    
    private static readonly MultiSelection _tabDoubleClickSelection = 
        MultiSelection.CreateMultiSelectionFromMelonPref(ModSettings.Entry_TabDoubleClick);

    public static Button placeStickersButton;

    #region Category Setup

    private static void Setup_StickersModCategory()
    {
        _ourCategory = _rootPage.AddMelonCategory(ModSettings.Hidden_Foldout_SettingsCategory);

        placeStickersButton = _ourCategory.AddButton("Place Stickers", "Stickers-magic-wand", "Place stickers via raycast.", ButtonStyle.TextWithIcon);
        placeStickersButton.OnPress += OnPlaceStickersButtonClick;

        Button clearSelfStickersButton = _ourCategory.AddButton("Clear Self", "Stickers-eraser", "Clear own stickers.", ButtonStyle.TextWithIcon);
        clearSelfStickersButton.OnPress += OnClearSelfStickersButtonClick;

        Button clearAllStickersButton = _ourCategory.AddButton("Clear All", "Stickers-rubbish-bin", "Clear all stickers.", ButtonStyle.TextWithIcon);
        clearAllStickersButton.OnPress += OnClearAllStickersButtonClick;

        Button openStickersFolderButton = _ourCategory.AddButton("Open Stickers Folder", "Stickers-folder", "Open UserData/Stickers folder in explorer. If above 256kb your image will automatically be downscaled for networking reasons.", ButtonStyle.TextWithIcon);
        openStickersFolderButton.OnPress += OnOpenStickersFolderButtonClick;

        Button openStickerSFXButton = _ourCategory.AddButton("Sticker SFX", "Stickers-headset", "Choose the SFX used when a sticker is placed.", ButtonStyle.TextWithIcon);
        openStickerSFXButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_sfxSelection);

        ToggleButton toggleDesktopKeybindButton = _ourCategory.AddToggle("Use Desktop Keybind", "Should the Desktop keybind be active.", ModSettings.Entry_UsePlaceBinding.Value);
        Button openDesktopKeybindButton = _ourCategory.AddButton("Desktop Keybind", "Stickers-alphabet", "Choose the key binding to place stickers.", ButtonStyle.TextWithIcon);
        openDesktopKeybindButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_desktopKeybindSelection);
        toggleDesktopKeybindButton.OnValueUpdated += (b) =>
        {
            ModSettings.Entry_UsePlaceBinding.Value = b;
            openDesktopKeybindButton.Disabled = !b;
        };

        Button openTabDoubleClickButton = _ourCategory.AddButton("Tab Double Click", "Stickers-mouse", "Choose the action to perform when double clicking the Stickers tab.", ButtonStyle.TextWithIcon);
        openTabDoubleClickButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_tabDoubleClickSelection);
    }

    #endregion Category Setup

    #region Button Actions

    private static void OnPlaceStickersButtonClick()
    {
        if (!_isOurTabOpened) return;

        if (StickerSystem.RestrictedInstance == false)
        {
            string mode = StickerSystem.Instance.IsInStickerMode ? "Exiting" : "Entering";
            QuickMenuAPI.ShowAlertToast($"{mode} sticker placement mode...", 2);
            StickerSystem.Instance.IsInStickerMode = !StickerSystem.Instance.IsInStickerMode;
        }
        else
        {
            QuickMenuAPI.ShowAlertToast("Stickers are not allowed in this world!", 2);
        }
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
    
    public static void UpdateStickerMenu() //TODO: add Icon changing, Bono needs to expose the value first.
    {
        if (StickerSystem.RestrictedInstance == true)
        {
            _rootPage.MenuSubtitle = "Stickers... are sadly disabled in this world.";

            placeStickersButton.Disabled = true;
            placeStickersButton.ButtonText = "Stickers Disabled";
            placeStickersButton.ButtonTooltip = "This world is not allowing Stickers.";
            placeStickersButton.ButtonIcon = "Stickers-magic-wand-broken";

        }
        else
        {
            _rootPage.MenuSubtitle = "Stickers! Double-click the tab to quickly toggle Sticker Mode.";

            placeStickersButton.Disabled = false;
            placeStickersButton.ButtonText = "Place Stickers";
            placeStickersButton.ButtonTooltip = "Place stickers via raycast.";
            placeStickersButton.ButtonIcon = "Stickers-magic-wand";
        }

    }

    #endregion Button Actions
}