using ABI_RC.Core.Player;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.OriginShift;

namespace NAK.OriginShiftMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static Category _ourCategory;
        
        private static Button _ourMainButton;
        private static bool _isForcedMode;
        
        private static ToggleButton _ourToggle;
        
        private static void Setup_OriginShiftModCategory(Page page)
        {
            // dear category
            _ourCategory = AddMelonCategory(ref page, ModSettings.OSM_SettingsCategory);
            
            // the button
            _ourMainButton = _ourCategory.AddButton(string.Empty, string.Empty, string.Empty, ButtonStyle.TextOnly);
            _ourMainButton.OnPress += OnMainButtonClick;
            SetButtonState(OriginShiftManager.OriginShiftState.Inactive); // default state
            
            // compatibility mode
            _ourToggle = _ourCategory.AddToggle(ModSettings.EntryCompatibilityMode.DisplayName,
                ModSettings.EntryCompatibilityMode.Description, ModSettings.EntryCompatibilityMode.Value);
            _ourToggle.OnValueUpdated += OnCompatibilityModeToggle;
            
            // listen for state changes
            OriginShiftManager.OnStateChanged += OnOriginShiftStateChanged;
        }

        #region Category Actions
        
        private static void UpdateCategoryModUserCount()
        {
            int modUsers = 1; // we are always here :3
            int playerCount = CVRPlayerManager.Instance.NetworkPlayers.Count + 1; // +1 for us :3
            _ourCategory.CategoryName = $"{ModSettings.OSM_SettingsCategory} ({modUsers}/{playerCount})";
        }

        #endregion Category Actions

        #region Button Actions

        private static void SetButtonState(OriginShiftManager.OriginShiftState state)
        {
            switch (state)
            {
                default:
                case OriginShiftManager.OriginShiftState.Inactive:
                    _ourMainButton.ButtonText = "Inactive";
                    _ourMainButton.ButtonIcon = "OSM_Icon_OriginShiftConfig";
                    _ourMainButton.ButtonTooltip = "World does not use Origin Shift.";
                    _ourMainButton.ButtonIcon = "OriginShift-Icon-Inactive";
                    break;
                case OriginShiftManager.OriginShiftState.Active:
                    _ourMainButton.ButtonText = "Active";
                    _ourMainButton.ButtonIcon = "OSM_Icon_OriginShiftConfig";
                    _ourMainButton.ButtonTooltip = "World uses Origin Shift.";
                    _ourMainButton.ButtonIcon = "OriginShift-Icon-Active";
                    break;
                case OriginShiftManager.OriginShiftState.Forced:
                    _ourMainButton.ButtonText = "Forced";
                    _ourMainButton.ButtonIcon = "OSM_Icon_OriginShiftCopy";
                    _ourMainButton.ButtonTooltip = "World is forced to use Origin Shift.";
                    _ourMainButton.ButtonIcon = "OriginShift-Icon-Forced";
                    break;
            }
        }
        
        private static void OnMainButtonClick()
        {
            // if active, return as world is using Origin Shift
            if (OriginShiftManager.Instance.CurrentState 
                is OriginShiftManager.OriginShiftState.Active)
                return;
            
            if (_isForcedMode)
            {
                OriginShiftManager.Instance.ResetManager();
            }
            else
            {
                QuickMenuAPI.ShowConfirm("Force Origin Shift",
                    "Are you sure you want to force Origin Shift for this world? " +
                    "This is a highly experimental feature that may break the world in unexpected ways!",
                () =>
                {
                    OriginShiftManager.Instance.ForceManager();
                });
            }
        }

        private static void OnOriginShiftStateChanged(OriginShiftManager.OriginShiftState state)
        {
            _isForcedMode = state == OriginShiftManager.OriginShiftState.Forced;
            SetButtonState(state);
            SetToggleLocked(_isForcedMode);
        }
        
        #endregion Button Actions

        #region Toggle Actions
        
        private static void SetToggleLocked(bool value)
        {
            if (value)
            {
                // lock the toggle
                _ourToggle.ToggleTooltip = "This setting is locked while Origin Shift is forced in Public instances.";
                _ourToggle.ToggleValue = true;
                _ourToggle.Disabled = true;
            }
            else
            {
                // unlock the toggle
                _ourToggle.ToggleValue = ModSettings.EntryCompatibilityMode.Value;
                _ourToggle.ToggleTooltip = ModSettings.EntryCompatibilityMode.Description;
                _ourToggle.Disabled = false;
            }
        }
        
        private static void OnCompatibilityModeToggle(bool value)
        {
            ModSettings.EntryCompatibilityMode.Value = value;
        }

        #endregion Toggle Actions
    }
}