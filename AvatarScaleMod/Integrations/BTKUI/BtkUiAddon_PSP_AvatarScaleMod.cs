using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.AvatarScaleMod.AvatarScaling;

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static Button _playerHasModElement;
        private static string _selectedPlayer;
        
        private static void Setup_PlayerSelectPage()
        {
            QuickMenuAPI.OnPlayerSelected += OnPlayerSelected;
            
            Category category = QuickMenuAPI.PlayerSelectPage.AddCategory(ModSettings.ASM_SettingsCategory, ModSettings.ModName);
            
            _playerHasModElement = category.AddButton("PLAYER_HAS_MOD", "ASM_Icon_AvatarHeightCopy", "PLAYER_HAS_MOD_TOOLTIP");
            
            Button button = category.AddButton("Copy Height", "ASM_Icon_AvatarHeightCopy", "Copy selected players Eye Height.");
            button.OnPress += OnCopyPlayerHeight;
        }

        #region QM Events
        
        private static void OnPlayerSelected(string _, string id)
        {
            _selectedPlayer = id;
            UpdatePlayerHasModIcon();
        }
        
        private static void OnCopyPlayerHeight()
        {
            float networkHeight = AvatarScaleManager.Instance.GetNetworkHeight(_selectedPlayer);
            if (networkHeight < 0) return;
            AvatarScaleManager.Instance.SetTargetHeight(networkHeight);
        }
        
        #endregion

        #region Private Methods

        private static void UpdatePlayerHasModIcon()
        {
            if (_playerHasModElement == null)
                return;
            
            if (AvatarScaleManager.Instance.DoesNetworkHeightScalerExist(_selectedPlayer))
            {
                _playerHasModElement.ButtonIcon = "ASM_Icon_AvatarHeightCopy";
                _playerHasModElement.ButtonText = "Player Has Mod";
                _playerHasModElement.ButtonTooltip = "This player has the Avatar Scale Mod installed!";
            }
            else
            {
                _playerHasModElement.ButtonIcon = "ASM_Icon_AvatarHeightConfig";
                _playerHasModElement.ButtonText = "Player Does Not Have Mod";
                _playerHasModElement.ButtonTooltip = "This player does not have the Avatar Scale Mod installed!";
            }
        }

        #endregion
    }
}