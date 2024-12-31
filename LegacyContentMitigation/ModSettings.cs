using MelonLoader;

namespace NAK.LegacyContentMitigation;

internal static class ModSettings
{
    #region Constants

    internal const string ModName = nameof(LegacyContentMitigation);
    internal const string LCM_SettingsCategory = "Legacy Content Mitigation";

    #endregion Constants

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    internal static readonly MelonPreferences_Entry<bool> EntryAutoForLegacyWorlds =
        Category.CreateEntry("auto_for_legacy_worlds", true,
            "Auto For Legacy Worlds", description: "Should Legacy View be auto enabled for detected Legacy worlds?");
    
    #endregion Melon Preferences
}