using System.Globalization;
using ABI_RC.Core.UI;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using MelonLoader;
using UnityEngine;

namespace NAK.ScrollFlight;

public class ScrollFlightMod : MelonMod
{
    #region Melon Preferences
    
    private const string SettingsCategory = nameof(ScrollFlightMod);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    private static readonly MelonPreferences_Entry<bool> EntryUseScrollFlight =
        Category.CreateEntry("use_scroll_flight", true,  
            "Use Scroll Flight", description: "Toggle Scroll Flight.");
    
    private static readonly MelonPreferences_Entry<bool> EntryResetOnExitFlight =
        Category.CreateEntry("reset_on_exit_flight", false,  
            "Reset On Exit Flight", description: "Reset Scroll Flight speed on exit flight.");
    
    #endregion Melon Preferences

    #region Private Fields

    private static float _currentWorldFlightSpeedMultiplier;
    private static float _originalWorldFlightSpeedMultiplier = 5f;

    #endregion Private Fields

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        CVRWorld.GameRulesUpdated += OnApplyMovementSettings; // thank you kafe for using actions
    }

    private bool wasFlying;
    
    // stole from LucMod :3
    public override void OnUpdate()
    {
        if (!EntryUseScrollFlight.Value) 
            return;
        
        if (BetterBetterCharacterController.Instance == null)
            return;
        
        bool isFlying = BetterBetterCharacterController.Instance.IsFlying();
        
        if (EntryResetOnExitFlight.Value 
            && (wasFlying && !isFlying))
        {
            BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier = _originalWorldFlightSpeedMultiplier;
            _currentWorldFlightSpeedMultiplier = _originalWorldFlightSpeedMultiplier;
        }
        
        wasFlying = isFlying;
        
        if (!isFlying
            || Input.GetKey(KeyCode.Mouse2) // scroll zoom (TODO: Use CVRInputManager.zoom, but requires fixing zoom toggle mode on client)
            || Input.GetKey(KeyCode.LeftControl) // third person / better interact desktop
            || Cursor.lockState != CursorLockMode.Locked) // unity explorer / in menu
            return;
        
        if (Input.mouseScrollDelta.y != 0f)
            AdjustFlightModifier(Input.mouseScrollDelta.y);
    }
    
    private static void AdjustFlightModifier(float adjustValue)
    {
        _currentWorldFlightSpeedMultiplier = Mathf.Max(0f, _currentWorldFlightSpeedMultiplier + adjustValue);

        if (_currentWorldFlightSpeedMultiplier <= 0f)
        {
            // reset to original value
            BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier = _originalWorldFlightSpeedMultiplier;
            CohtmlHud.Instance.ViewDropTextImmediate("(Local) ScrollFlight",
                "Default", $"World default ({_currentWorldFlightSpeedMultiplier.ToString(CultureInfo.InvariantCulture)})");
            return;
        }
        
        BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier = _currentWorldFlightSpeedMultiplier;
        CohtmlHud.Instance.ViewDropTextImmediate("(Local) ScrollFlight",
            _currentWorldFlightSpeedMultiplier.ToString(CultureInfo.InvariantCulture), "Speed multiplier");
    }

    #endregion Melon Events
    
    #region Harmony Patches

    private static void OnApplyMovementSettings()
    {
        _currentWorldFlightSpeedMultiplier = _originalWorldFlightSpeedMultiplier = BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier;
    }

    #endregion Harmony Patches
}