using MelonLoader;

namespace NAK.OriginShift;

internal static class ModSettings
{
    #region Constants

    internal const string ModName = nameof(OriginShift);
    internal const string OSM_SettingsCategory = "Origin Shift Mod";

    #endregion Constants

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    internal static readonly MelonPreferences_Entry<bool> EntryCompatibilityMode =
        Category.CreateEntry("EntryCompatibilityMode", true,
            "Compatibility Mode", description: "Origin Shifts locally, but modifies outbound network messages to be compatible with non-Origin Shifted clients.");
    
    #endregion Melon Preferences

    #region Settings Managment
    
    internal static void Initialize()
    {
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
        
        OnSettingsChanged();
    }

    private static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        OriginShiftManager.CompatibilityMode = EntryCompatibilityMode.Value;
    }
    
    #endregion Settings Managment
}