using MelonLoader;
using UnityEngine;

namespace NAK.Stickers;

public static class ModSettings
{
    #region Constants & Category

    internal const string ModName = nameof(StickerMod);
    
    internal const string SM_SettingsCategory = "Stickers Mod";
    private const string SM_SelectionCategory = "Sticker Selection";
    private const string DEBUG_SettingsCategory = "Debug Options";

    internal const int MaxStickerSlots = 4;
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    #endregion Constants & Category

    #region Hidden Foldout Entries

    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SettingsCategory =
        Category.CreateEntry("hidden_foldout_config", true, is_hidden: true, display_name: SM_SettingsCategory, description: "Foldout state for Sticker Mod settings.");
    
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SelectionCategory =
        Category.CreateEntry("hidden_foldout_selection", true, is_hidden: true, display_name: SM_SelectionCategory, description: "Foldout state for Sticker selection.");
    
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_DebugCategory =
        Category.CreateEntry("hidden_foldout_debug", false, is_hidden: true, display_name: DEBUG_SettingsCategory, description: "Foldout state for Debug settings.");

    #endregion Hidden Foldout Entries
    
    #region Stickers Mod Settings
    
    internal static readonly MelonPreferences_Entry<bool> Entry_HapticsOnPlace =
        Category.CreateEntry("haptics_on_place", true, "Haptics On Place", "Enable haptic feedback when placing stickers.");
    
    internal static readonly MelonPreferences_Entry<float> Entry_PlayerUpAlignmentThreshold =
        Category.CreateEntry("player_up_alignment_threshold", 20f, "Player Up Alignment Threshold", "The threshold the controller roll can be within to align perfectly with the player up vector. Set to 0f to always align to controller up.");
    
    internal static readonly MelonPreferences_Entry<SFXType> Entry_SelectedSFX =
        Category.CreateEntry("selected_sfx", SFXType.LittleBigPlanetSticker, "Selected SFX", "The SFX used when a sticker is placed.");
    
    internal static readonly MelonPreferences_Entry<bool> Entry_UsePlaceBinding =
        Category.CreateEntry("use_binding", true, "Use Place Binding", "Use the place binding to place stickers.");
    
    internal static readonly MelonPreferences_Entry<KeyBind> Entry_PlaceBinding =
        Category.CreateEntry("place_binding", KeyBind.G, "Sticker Bind", "The key binding to place stickers.");
    
    internal static readonly MelonPreferences_Entry<TabDoubleClick> Entry_TabDoubleClick =
        Category.CreateEntry("tab_double_click", TabDoubleClick.ToggleStickerMode, "Tab Double Click", "The action to perform when double clicking the Stickers tab.");
    
    internal static readonly MelonPreferences_Entry<string[]> Hidden_SelectedStickerNames =
        Category.CreateEntry("selected_sticker_name", Array.Empty<string>(), 
            display_name: "Selected Sticker Name", 
            description: "The name of the sticker selected for stickering.", 
            is_hidden: true);

    #endregion Stickers Mod Settings
    
    #region Debug Settings

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Inbound", description: "Log inbound Mod Network updates.");

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Outbound", description: "Log outbound Mod Network updates.");

    #endregion Debug Settings

    #region Initialization
    
    internal static void Initialize()
    {
        // ensure sticker slots are initialized to the correct size
        string[] selectedStickerNames = Hidden_SelectedStickerNames.Value;
        if (selectedStickerNames.Length != MaxStickerSlots) Array.Resize(ref selectedStickerNames, MaxStickerSlots);
        Hidden_SelectedStickerNames.Value = selectedStickerNames;

        foreach (var selectedSticker in selectedStickerNames)
            StickerMod.Logger.Msg($"Selected Sticker: {selectedSticker}");
        
        Entry_PlayerUpAlignmentThreshold.OnEntryValueChanged.Subscribe(OnPlayerUpAlignmentThresholdChanged);
    }
    
    #endregion Initialization

    #region Setting Changed Callbacks
    
    private static void OnPlayerUpAlignmentThresholdChanged(float oldValue, float newValue)
    {
        Entry_PlayerUpAlignmentThreshold.Value = Mathf.Clamp(newValue, 0f, 180f);
    }

    #endregion Setting Changed Callbacks
}