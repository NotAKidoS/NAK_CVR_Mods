using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;
using NAK.AvatarScaleMod.Networking;

namespace NAK.AvatarScaleMod;

// i like this

internal static class ModSettings
{
    // Constants
    internal const string ModName = nameof(AvatarScaleMod);
    internal const string ASM_SettingsCategory = "Avatar Scale Mod";
    internal const string AST_SettingsCategory = "Avatar Scale Tool Support";
    internal const string USM_SettingsCategory = "Universal Scaling (Mod Network)";
    internal const string DEBUG_SettingsCategory = "Debug Options";
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);

    #region Hidden Foldout Entries

    // Avatar Scale Mod Foldout
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_ASM_SettingsCategory =
        Category.CreateEntry("hidden_foldout_asm", true, is_hidden: true, display_name: ASM_SettingsCategory, description: "Foldout state for Avatar Scale Mod settings.");

    // Avatar Scale Tool Foldout
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_AST_SettingsCategory =
        Category.CreateEntry("hidden_foldout_ast", false, is_hidden: true, display_name: AST_SettingsCategory, description: "Foldout state for Avatar Scale Tool settings.");
    
    // Universal Scaling (Mod Network) Foldout
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_USM_SettingsCategory =
        Category.CreateEntry("hidden_foldout_usm", false, is_hidden: true, display_name: USM_SettingsCategory, description: "Foldout state for Universal Scaling (Mod Network) settings.");
    
    // Debug Options Foldout
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_DEBUG_SettingsCategory =
        Category.CreateEntry("hidden_foldout_debug", false, is_hidden: true, display_name: DEBUG_SettingsCategory, description: "Foldout state for Debug Options settings.");

    // Player Select Page Foldout
    internal static readonly MelonPreferences_Entry<bool> Hidden_Foldout_PlayerSelectPage =
        Category.CreateEntry("hidden_foldout_player_select_page", true, is_hidden: true, display_name: ASM_SettingsCategory, description: "Foldout state for Player Select Page.");

    #endregion
    
    #region Avatar Scale Mod Settings
    
    public static readonly MelonPreferences_Entry<bool> EntryScaleGestureEnabled =
        Category.CreateEntry("scale_gesture_enabled", true, display_name: "Scale Gesture", description: "Enable or disable scale gesture.");
    
    public static readonly MelonPreferences_Entry<bool> EntryScaleKeybindingsEnabled =
        Category.CreateEntry("scale_keybindings_enabled", true, display_name: "Scale Keybindings", description: "Enable or disable scale keybindings.");
    
    public static readonly MelonPreferences_Entry<bool> EntryPersistentHeight =
        Category.CreateEntry("persistent_height", false, display_name: "Persistent Height", description: "Should the avatar height persist between avatar switches?");
    
    public static readonly MelonPreferences_Entry<bool> EntryPersistThroughRestart =
        Category.CreateEntry("persistent_height_through_restart", false, display_name: "Persist Through Restart", description: "Should the avatar height persist between game restarts?");
    
    // stores the last avatar height as a melon pref
    public static readonly MelonPreferences_Entry<float> EntryHiddenAvatarHeight =
        Category.CreateEntry("hidden_avatar_height", -2f, is_hidden: true, display_name: "Avatar Height", description: "Set your avatar height.");
    
    #endregion
    
    #region Avatar Scale Tool Settings
    
    public static readonly MelonPreferences_Entry<string> EntryASTScaleParameter =
        Category.CreateEntry("override_scale_parameter", "AvatarScale", display_name: "Override Scale Parameter", description: "Override the scale parameter on the avatar.");
    
    public static readonly MelonPreferences_Entry<float> EntryASTMinHeight =
        Category.CreateEntry("override_min_height", 0.25f, display_name: "Override Min Height", description: "Override the minimum height.");
    
    public static readonly MelonPreferences_Entry<float> EntryASTMaxHeight =
        Category.CreateEntry("override_max_height", 2.5f, display_name: "Override Max Height", description: "Override the maximum height.");
    
    #endregion
    
    #region Universal Scaling Settings
    
    public static readonly MelonPreferences_Entry<bool> EntryUseUniversalScaling =
        Category.CreateEntry("use_universal_scaling", false, display_name: "Use Universal Scaling", description: "Enable or disable universal scaling. This allows scaling to work on any avatar as well as networking via Mod Network.");
    
    public static readonly MelonPreferences_Entry<bool> EntryScaleComponents =
        Category.CreateEntry("scale_components", true, display_name: "Scale Components", description: "Scale components on the avatar. (Constraints, Audio Sources, etc.)");
    
    public static readonly MelonPreferences_Entry<bool> EntryAnimationScalingOverride =
        Category.CreateEntry("allow_anim_clip_scale_override", true, display_name: "Animation-Clip Scaling Override", description: "Allow animation-clip scaling to override universal scaling.");
    
    #endregion
    
    #region Debug Settings
    
    public static readonly MelonPreferences_Entry<bool> Debug_NetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Inbound", description: "Log inbound Mod Network height updates.");

    public static readonly MelonPreferences_Entry<bool> Debug_NetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Outbound", description: "Log outbound Mod Network height updates.");

    public static readonly MelonPreferences_Entry<bool> Debug_ComponentSearchTime =
        Category.CreateEntry("debug_component_search_time", false, display_name: "Debug Search Time", description: "Log component search time.");
    
    #endregion
    
    public static void Initialize()
    {
        // subscribe to all bool settings that aren't hidden
        foreach (MelonPreferences_Entry entry in Category.Entries.Where(entry => entry.GetReflectedType() == typeof(bool) && !entry.IsHidden))
            entry.OnEntryValueChangedUntyped.Subscribe(OnSettingsBoolChanged);
    }

    private static void OnSettingsBoolChanged(object _, object __)
    {
        GestureReconizer.ScaleReconizer.Enabled = EntryScaleGestureEnabled.Value;
        ModNetwork.Debug_NetworkInbound = Debug_NetworkInbound.Value;
        ModNetwork.Debug_NetworkOutbound = Debug_NetworkOutbound.Value;

        if (AvatarScaleManager.Instance == null) return;
        AvatarScaleManager.Instance.Setting_UniversalScaling = EntryUseUniversalScaling.Value;
        AvatarScaleManager.Instance.Setting_AnimationClipScalingOverride = EntryAnimationScalingOverride.Value;
        AvatarScaleManager.Instance.Setting_PersistentHeight = EntryPersistentHeight.Value;
    }
}