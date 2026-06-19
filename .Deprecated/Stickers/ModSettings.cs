using MelonLoader;
using UnityEngine;

namespace NAK.Stickers;

public static class ModSettings
{
    #region Constants & Category

    internal const string ModName = nameof(StickerMod);
    
    internal const string SM_SettingsCategory = "Stickers Mod";
    private const string SM_SelectionCategory = "Sticker Selection";
    private const string MISC_SettingsCategory = "Miscellaneous Options";

    internal const int MaxStickerSlots = 4;
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    #endregion Constants & Category

    #region Hidden Foldout Entries

    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SettingsCategory =
        Category.CreateEntry("hidden_foldout_config", true, is_hidden: true, display_name: SM_SettingsCategory, description: "Foldout state for Sticker Mod settings.");
    
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SelectionCategory =
        Category.CreateEntry("hidden_foldout_selection", true, is_hidden: true, display_name: SM_SelectionCategory, description: "Foldout state for Sticker selection.");

    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_MiscCategory =
        Category.CreateEntry("hidden_foldout_miscellaneous", false, is_hidden: true, display_name: MISC_SettingsCategory, description: "Foldout state for Miscellaneous settings.");

    #endregion Hidden Foldout Entries
    
    #region Stickers Mod Settings
    
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
        Category.CreateEntry("selected_sticker_name", new[] { "", "", "", "" }, 
            display_name: "Selected Sticker Name", 
            description: "The name of the sticker selected for stickering.", 
            is_hidden: true);
    
    internal static readonly MelonPreferences_Entry<bool> Entry_FriendsOnly =
        Category.CreateEntry("friends_only", false, "Friends Only", "Only allow friends to use stickers.");

	internal static readonly MelonPreferences_Entry<StickerSize> Entry_StickerSize =
        Category.CreateEntry("sticker_size", StickerSize.Chonk, "Sticker Size", "The size of the sticker when placed.");
    
    internal static readonly MelonPreferences_Entry<float> Entry_StickerOpacity =
        Category.CreateEntry("opacity", 1f, "Opacity", "The opacity of the sticker when placed.");

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
        
        // ensure theres no null entries so toml shuts the fuck up
        for (int i = 0; i < selectedStickerNames.Length; i++)
            selectedStickerNames[i] ??= "";

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