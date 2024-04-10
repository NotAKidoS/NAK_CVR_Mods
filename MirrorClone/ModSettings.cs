using MelonLoader;
using UnityEngine;

namespace NAK.BetterShadowClone;

public static class ModSettings
{
    #region Melon Prefs

    private const string SettingsCategory = nameof(MirrorCloneMod);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    internal static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true,
            description: "Enable Mirror Clone.");

    #endregion

    internal static void Initialize()
    {
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }
    
    internal static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
     
    }
}