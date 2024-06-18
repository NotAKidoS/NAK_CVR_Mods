using ABI_RC.Core.Player;
using BTKUILib;
using BTKUILib.UIObjects;
using NAK.OriginShift;

namespace NAK.OriginShiftMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static Page _miscTabPage;
        private static string _miscTabElementID;

        public static void Initialize()
        {
            Prepare_Icons();
            Setup_OriginShiftTab();
        }

        #region Initialization

        private static void Prepare_Icons()
        {
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "OriginShift-Icon-Active",
                GetIconStream("OriginShift-Icon-Active.png"));
            
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "OriginShift-Icon-Inactive",
                GetIconStream("OriginShift-Icon-Inactive.png"));         
            
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "OriginShift-Icon-Forced",
                GetIconStream("OriginShift-Icon-Forced.png"));
        }

        private static void Setup_OriginShiftTab()
        {
            _miscTabPage = QuickMenuAPI.MiscTabPage;
            _miscTabElementID = _miscTabPage.ElementID;
            QuickMenuAPI.UserJoin += OnUserJoinLeave;
            QuickMenuAPI.UserLeave += OnUserJoinLeave;
            QuickMenuAPI.OnWorldLeave += OnWorldLeave;
            
            // // Origin Shift Mod
            Setup_OriginShiftModCategory(_miscTabPage);
            //
            // // Origin Shift Tool
            // Setup_OriginShiftToolCategory(_miscTabPage);
            //
            // // Universal Shifting Settings
            // Setup_UniversalShiftingSettings(_miscTabPage);
            //
            // // Debug Options
            // Setup_DebugOptionsCategory(_miscTabPage);
        }

        #endregion

        #region Player Count Display

        private static void OnWorldLeave()
            => UpdateCategoryModUserCount();
        
        private static void OnUserJoinLeave(CVRPlayerEntity _)
            => UpdateCategoryModUserCount();
        
        #endregion
    }
}
