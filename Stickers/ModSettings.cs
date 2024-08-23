using MelonLoader;
using UnityEngine;

namespace NAK.Stickers;

public static class ModSettings
{
    internal const string ModName = nameof(StickerMod);
    internal const string SM_SettingsCategory = "Stickers Mod";
    internal const string SM_SelectionCategory = "Sticker Selection";
    private const string DEBUG_SettingsCategory = "Debug Options";

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);

    #region Hidden Foldout Entries

    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SettingsCategory =
        Category.CreateEntry("hidden_foldout_config", true, is_hidden: true, display_name: SM_SettingsCategory, description: "Foldout state for Sticker Mod settings.");
    
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_SelectionCategory =
        Category.CreateEntry("hidden_foldout_selection", true, is_hidden: true, display_name: SM_SelectionCategory, description: "Foldout state for Sticker selection.");
    
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_DebugCategory =
        Category.CreateEntry("hidden_foldout_debug", false, is_hidden: true, display_name: DEBUG_SettingsCategory, description: "Foldout state for Debug settings.");

    #endregion Hidden Foldout Entries
    
    #region Stickers Mod Settings
    
    internal static readonly MelonPreferences_Entry<float> Entry_PlayerUpAlignmentThreshold =
        Category.CreateEntry("player_up_alignment_threshold", 20f, "Player Up Alignment Threshold", "The threshold the controller roll can be within to align perfectly with the player up vector. Set to 0f to always align to controller up.");
    
    internal static readonly MelonPreferences_Entry<SFXType> Entry_SelectedSFX =
        Category.CreateEntry("selected_sfx", SFXType.LBP, "Selected SFX", "The SFX used when a sticker is placed.");
    
    internal enum SFXType
    {
        LBP,
        Source,
        None
    }
    
    internal static readonly MelonPreferences_Entry<bool> Entry_UsePlaceBinding =
        Category.CreateEntry("use_binding", true, "Use Place Binding", "Use the place binding to place stickers.");
    
    internal static readonly MelonPreferences_Entry<KeyCode> Entry_PlaceBinding =
        Category.CreateEntry("place_binding", KeyCode.G, "Sticker Bind", "The key binding to place stickers.");

    internal static readonly MelonPreferences_Entry<string> Hidden_SelectedStickerName =
        Category.CreateEntry("hidden_selected_sticker", string.Empty, is_hidden: true, display_name: "Selected Sticker", description: "The currently selected sticker name.");

    #endregion Stickers Mod Settings

    #region Decalery Settings

    internal static readonly MelonPreferences_Entry<DecaleryMode> Decalery_DecalMode =
        Category.CreateEntry("decalery_decal_mode", DecaleryMode.GPU, display_name: "Decal Mode", description: "The mode Decalery should use for decal creation. By default GPU should be used. **Note:** Not all content is marked as readable, so only the GPU modes are expected to work properly on UGC.");

    internal enum DecaleryMode
    {
        CPU,
        GPU,
        CPUBurst,
        GPUIndirect
    }
    
    #endregion Decalery Settings
    
    #region Debug Settings

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Inbound", description: "Log inbound Mod Network updates.");

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Outbound", description: "Log outbound Mod Network updates.");

    #endregion Debug Settings

    #region Initialization
    
    internal static void Initialize()
    {
        Entry_PlayerUpAlignmentThreshold.OnEntryValueChanged.Subscribe(OnPlayerUpAlignmentThresholdChanged);
        Decalery_DecalMode.OnEntryValueChanged.Subscribe(OnDecaleryDecalModeChanged);
    }
    
    #endregion Initialization

    #region Setting Changed Callbacks
    
    private static void OnPlayerUpAlignmentThresholdChanged(float oldValue, float newValue)
    {
        Entry_PlayerUpAlignmentThreshold.Value = Mathf.Clamp(newValue, 0f, 180f);
    }
    
    private static void OnDecaleryDecalModeChanged(DecaleryMode oldValue, DecaleryMode newValue)
    {
        DecalManager.SetPreferredMode((DecalUtils.Mode)newValue, newValue == DecaleryMode.GPUIndirect, 0);
        if (newValue != DecaleryMode.GPU) StickerMod.Logger.Warning("Decalery is not set to GPU mode. Expect compatibility issues with user generated content when mesh data is not marked as readable.");
        StickerSystem.Instance.CleanupAll();
    }

    #endregion Setting Changed Callbacks
}