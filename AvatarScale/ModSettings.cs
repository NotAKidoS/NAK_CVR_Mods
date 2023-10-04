using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;
using NAK.AvatarScaleMod.Networking;

namespace NAK.AvatarScaleMod;

// i like this

internal static class ModSettings
{
    // Constants
    internal const string ModName = nameof(AvatarScaleMod);
    internal const string SettingsCategory = "Avatar Scale Mod";

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    public static readonly MelonPreferences_Entry<bool> EntryUseUniversalScaling =
        Category.CreateEntry("use_universal_scaling", true, display_name: "Use Universal Scaling", description: "Enable or disable universal scaling.");
    
    public static readonly MelonPreferences_Entry<bool> EntryPersistantHeight =
        Category.CreateEntry("persistant_height", false, display_name: "Persistant Height", description: "Should the avatar height persist between avatar switches?");
    
    public static readonly MelonPreferences_Entry<bool> EntryScaleGestureEnabled =
        Category.CreateEntry("scale_gesture_enabled", true, display_name: "Scale Gesture Enabled", description: "Enable or disable scale gesture.");
    
    public static readonly MelonPreferences_Entry<bool> EntryDebugNetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Inbound", description: "Log inbound Mod Network height updates.");

    public static readonly MelonPreferences_Entry<bool> EntryDebugNetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Outbound", description: "Log outbound Mod Network height updates.");

    public static readonly MelonPreferences_Entry<float> EntryHiddenLastAvatarScale =
        Category.CreateEntry("last_avatar_scale", -1f, is_hidden: true);

    public static void Initialize()
    {
        foreach (MelonPreferences_Entry entry in Category.Entries)
            entry.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }

    private static void OnSettingsChanged(object _, object __)
    {
        AvatarScaleManager.Instance.Setting_UniversalScaling = EntryUseUniversalScaling.Value;
        AvatarScaleManager.Instance.Setting_PersistantHeight = EntryPersistantHeight.Value;

        GestureReconizer.ScaleReconizer.Enabled = EntryScaleGestureEnabled.Value;

        ModNetwork.Debug_NetworkInbound = EntryDebugNetworkInbound.Value;
        ModNetwork.Debug_NetworkOutbound = EntryDebugNetworkOutbound.Value;
    }
}
