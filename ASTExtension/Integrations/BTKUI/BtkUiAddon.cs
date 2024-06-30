using ABI_RC.Core.Player;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;

namespace NAK.ASTExtension.Integrations
{
    public static partial class BtkUiAddon
    {
        public static void Initialize()
        {
            Prepare_Icons();
            Setup_PlayerSelectPage();
        }
        
        private static void Prepare_Icons()
        {
            QuickMenuAPI.PrepareIcon(ASTExtensionMod.ModName, "ASM_Icon_AvatarHeightCopy",
                GetIconStream("ASM_Icon_AvatarHeightCopy.png"));
        }

        #region Player Select Page

        private static string _selectedPlayer;
        
        private static void Setup_PlayerSelectPage()
        {
            QuickMenuAPI.OnPlayerSelected += OnPlayerSelected;
            Category category = QuickMenuAPI.PlayerSelectPage.AddCategory(ASTExtensionMod.ModName, ASTExtensionMod.ModName);
            Button button = category.AddButton("Copy Height", "ASM_Icon_AvatarHeightCopy", "Copy selected players Eye Height.");
            button.OnPress += OnCopyPlayerHeight;
        }
        
        private static void OnPlayerSelected(string _, string id)
        {
            _selectedPlayer = id;
        }
        
        private static void OnCopyPlayerHeight()
        {
            if (string.IsNullOrEmpty(_selectedPlayer))
                return;
            
            if (!CVRPlayerManager.Instance.GetPlayerPuppetMaster(_selectedPlayer, out PuppetMaster player))
                return;
            
            if (player._avatar == null)
                return;

            float height = player.netIkController.GetRemoteHeight();
            ASTExtensionMod.Instance.SetAvatarHeight(height);
        }
        
        #endregion Player Select Page
    }
}