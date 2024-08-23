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
    
    internal static readonly MelonPreferences_Entry<KeyBind> Entry_PlaceBinding =
        Category.CreateEntry("place_binding", KeyBind.G, "Sticker Bind", "The key binding to place stickers.");
    
    internal enum KeyBind
    {
        // Alphabetic keys
        A = KeyCode.A, // 0x00000061
        B = KeyCode.B, // 0x00000062
        C = KeyCode.C, // 0x00000063
        D = KeyCode.D, // 0x00000064
        E = KeyCode.E, // 0x00000065
        F = KeyCode.F, // 0x00000066
        G = KeyCode.G, // 0x00000067
        H = KeyCode.H, // 0x00000068
        I = KeyCode.I, // 0x00000069
        J = KeyCode.J, // 0x0000006A
        K = KeyCode.K, // 0x0000006B
        L = KeyCode.L, // 0x0000006C
        M = KeyCode.M, // 0x0000006D
        N = KeyCode.N, // 0x0000006E
        O = KeyCode.O, // 0x0000006F
        P = KeyCode.P, // 0x00000070
        Q = KeyCode.Q, // 0x00000071
        R = KeyCode.R, // 0x00000072
        S = KeyCode.S, // 0x00000073
        T = KeyCode.T, // 0x00000074
        U = KeyCode.U, // 0x00000075
        V = KeyCode.V, // 0x00000076
        W = KeyCode.W, // 0x00000077
        X = KeyCode.X, // 0x00000078
        Y = KeyCode.Y, // 0x00000079
        Z = KeyCode.Z, // 0x0000007A
        
        // Mouse Buttons
        Mouse0 = KeyCode.Mouse0, // 0x00000143
        Mouse1 = KeyCode.Mouse1, // 0x00000144
        Mouse2 = KeyCode.Mouse2, // 0x00000145
        Mouse3 = KeyCode.Mouse3, // 0x00000146
        Mouse4 = KeyCode.Mouse4, // 0x00000147
        Mouse5 = KeyCode.Mouse5, // 0x00000148
        Mouse6 = KeyCode.Mouse6, // 0x00000149

        // Special Characters
        // Backspace = KeyCode.Backspace, // 0x00000008
        // Tab = KeyCode.Tab, // 0x00000009
        // Clear = KeyCode.Clear, // 0x0000000C
        // Return = KeyCode.Return, // 0x0000000D
        // Pause = KeyCode.Pause, // 0x00000013
        // Escape = KeyCode.Escape, // 0x0000001B
        // Space = KeyCode.Space, // 0x00000020
        // Exclaim = KeyCode.Exclaim, // 0x00000021
        // DoubleQuote = KeyCode.DoubleQuote, // 0x00000022
        // Hash = KeyCode.Hash, // 0x00000023
        // Dollar = KeyCode.Dollar, // 0x00000024
        // Percent = KeyCode.Percent, // 0x00000025
        // Ampersand = KeyCode.Ampersand, // 0x00000026
        // Quote = KeyCode.Quote, // 0x00000027
        // LeftParen = KeyCode.LeftParen, // 0x00000028
        // RightParen = KeyCode.RightParen, // 0x00000029
        // Asterisk = KeyCode.Asterisk, // 0x0000002A
        // Plus = KeyCode.Plus, // 0x0000002B
        // Comma = KeyCode.Comma, // 0x0000002C
        // Minus = KeyCode.Minus, // 0x0000002D
        // Period = KeyCode.Period, // 0x0000002E
        // Slash = KeyCode.Slash, // 0x0000002F
        // Alpha0 = KeyCode.Alpha0, // 0x00000030
        // Alpha1 = KeyCode.Alpha1, // 0x00000031
        // Alpha2 = KeyCode.Alpha2, // 0x00000032
        // Alpha3 = KeyCode.Alpha3, // 0x00000033
        // Alpha4 = KeyCode.Alpha4, // 0x00000034
        // Alpha5 = KeyCode.Alpha5, // 0x00000035
        // Alpha6 = KeyCode.Alpha6, // 0x00000036
        // Alpha7 = KeyCode.Alpha7, // 0x00000037
        // Alpha8 = KeyCode.Alpha8, // 0x00000038
        // Alpha9 = KeyCode.Alpha9, // 0x00000039
        // Colon = KeyCode.Colon, // 0x0000003A
        // Semicolon = KeyCode.Semicolon, // 0x0000003B
        // Less = KeyCode.Less, // 0x0000003C
        // Equals = KeyCode.Equals, // 0x0000003D
        // Greater = KeyCode.Greater, // 0x0000003E
        // Question = KeyCode.Question, // 0x0000003F
        // At = KeyCode.At, // 0x00000040
        // LeftBracket = KeyCode.LeftBracket, // 0x0000005B
        // Backslash = KeyCode.Backslash, // 0x0000005C
        // RightBracket = KeyCode.RightBracket, // 0x0000005D
        // Caret = KeyCode.Caret, // 0x0000005E
        // Underscore = KeyCode.Underscore, // 0x0000005F
        // BackQuote = KeyCode.BackQuote, // 0x00000060
        // Delete = KeyCode.Delete // 0x0000007F
    }
    
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