using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.Stickers.Networking;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    #region Setup
    
    private static ToggleButton _disallowForSessionButton;
    
    private static void Setup_PlayerOptionsPage()
    {
        Category category = QuickMenuAPI.PlayerSelectPage.AddCategory(ModSettings.SM_SettingsCategory, ModSettings.ModName);
        
        //Button identifyButton = category.AddButton("Identify Stickers", "Stickers-magnifying-glass", "Identify this players stickers by making them flash.");
        //identifyButton.OnPress += OnPressIdentifyPlayerStickersButton;
        
        Button clearStickersButton = category.AddButton("Clear Stickers", "Stickers-eraser", "Clear this players stickers.");
        clearStickersButton.OnPress += OnPressClearSelectedPlayerStickersButton;
        
        _disallowForSessionButton = category.AddToggle("Block for Session", "Disallow this player from using stickers for this session. This setting will not persist through restarts.", false);
        _disallowForSessionButton.OnValueUpdated += OnToggleDisallowForSessionButton;
        QuickMenuAPI.OnPlayerSelected += (_, id) => { _disallowForSessionButton.ToggleValue = ModNetwork.IsPlayerACriminal(id); };
    }
    
    #endregion Setup

    #region Callbacks
    
    // private static void OnPressIdentifyPlayerStickersButton()
    // {
    //     if (string.IsNullOrEmpty(QuickMenuAPI.SelectedPlayerID)) return;
    //     StickerSystem.Instance.OnStickerIdentifyReceived(QuickMenuAPI.SelectedPlayerID);
    // }
    
    private static void OnPressClearSelectedPlayerStickersButton()
    {
        if (string.IsNullOrEmpty(QuickMenuAPI.SelectedPlayerID)) return;
        StickerSystem.Instance.OnStickerClearAllReceived(QuickMenuAPI.SelectedPlayerID);
    }
    
    private static void OnToggleDisallowForSessionButton(bool isOn)
    {
        if (string.IsNullOrEmpty(QuickMenuAPI.SelectedPlayerID)) return;
        ModNetwork.HandleDisallowForSession(QuickMenuAPI.SelectedPlayerID, isOn);
    }


    #endregion Callbacks
}