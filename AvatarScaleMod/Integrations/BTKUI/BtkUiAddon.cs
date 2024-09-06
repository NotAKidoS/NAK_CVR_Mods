using ABI_RC.Core.Player;
using BTKUILib;
using BTKUILib.UIObjects;
using NAK.AvatarScaleMod.AvatarScaling;

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static Page _asmRootPage;
        private static string _rootPageElementID;

        public static void Initialize()
        {
            Prepare_Icons();
            Setup_AvatarScaleModTab();
            Setup_PlayerSelectPage();
        }

        #region Initialization

        private static void Prepare_Icons()
        {
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "ASM_Icon_AvatarHeightConfig",
                GetIconStream("ASM_Icon_AvatarHeightConfig.png"));
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "ASM_Icon_AvatarHeightCopy",
                GetIconStream("ASM_Icon_AvatarHeightCopy.png"));
        }

        private static void Setup_AvatarScaleModTab()
        {
            _asmRootPage = new Page(ModSettings.ModName, ModSettings.ASM_SettingsCategory, true, "ASM_Icon_AvatarHeightConfig")
            {
                MenuTitle = ModSettings.ASM_SettingsCategory,
                MenuSubtitle = "Everything Avatar Scaling!"
            };
            
            _rootPageElementID = _asmRootPage.ElementID;
            QuickMenuAPI.OnTabChange += OnTabChange;
            QuickMenuAPI.UserJoin += OnUserJoinLeave;
            QuickMenuAPI.UserLeave += OnUserJoinLeave;
            QuickMenuAPI.OnWorldLeave += OnWorldLeave;
            
            // Avatar Scale Mod
            Setup_AvatarScaleModCategory(_asmRootPage);

            // Avatar Scale Tool
            Setup_AvatarScaleToolCategory(_asmRootPage);

            // Universal Scaling Settings
            Setup_UniversalScalingSettings(_asmRootPage);

            // Debug Options
            Setup_DebugOptionsCategory(_asmRootPage);
        }

        #endregion

        #region Player Count Display

        private static void OnWorldLeave()
            => UpdatePlayerCountDisplay();
        
        private static void OnUserJoinLeave(CVRPlayerEntity _)
            => UpdatePlayerCountDisplay();
        
        private static void UpdatePlayerCountDisplay()
        {
            if (_asmRootPage == null)
                return;
            
            int modUserCount = AvatarScaleManager.Instance.GetNetworkHeightScalerCount();
            int playerCount = CVRPlayerManager.Instance.NetworkPlayers.Count;
            _asmRootPage.MenuSubtitle = $"Everything Avatar Scaling! :: ({modUserCount}/{playerCount} players using ASM)";
        }

        #endregion

        #region Double-Click Reset Height

        private static DateTime lastTime = DateTime.Now;

        private static void OnTabChange(string newTab, string previousTab)
        {
            if (newTab == _rootPageElementID)
            {
                UpdatePlayerCountDisplay();
                TimeSpan timeDifference = DateTime.Now - lastTime;
                if (timeDifference.TotalSeconds <= 0.5)
                {
                    AvatarScaleManager.Instance.Setting_UniversalScaling = false;
                    return;
                }
            }
            lastTime = DateTime.Now;
        }

        #endregion
    }
}