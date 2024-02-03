using MelonLoader;
using UnityEngine;

namespace NAK.BetterShadowClone;

public static class ModSettings
{
    #region Melon Prefs

    private const string SettingsCategory = nameof(ShadowCloneMod);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    internal static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true,
            description: "Enable Mirror Clone.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryUseShadowClone =
        Category.CreateEntry("Use Shadow Clone", true,
            description: "Should you have shadow clones?");
    
    internal static readonly MelonPreferences_Entry<bool> EntryCopyMaterialToShadow =
        Category.CreateEntry("Copy Material to Shadow", true,
            description: "Should the shadow clone copy the material from the original mesh? Note: This can have a slight performance hit.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryDontRespectFPR =
        Category.CreateEntry("Dont Respect FPR", false,
            description: "Should the transform hider not respect FPR?");
    
    internal static readonly MelonPreferences_Entry<bool> EntryDebugHeadHide =
        Category.CreateEntry("Debug Head Hide", false,
            description: "Should head be hidden for first render?");


    #endregion

    internal static void Initialize()
    {
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }

    private static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        TransformHiderManager.s_DisallowFprExclusions = EntryDontRespectFPR.Value;
        TransformHiderManager.s_DebugHeadHide = EntryDebugHeadHide.Value;
        ShadowCloneManager.s_CopyMaterialsToShadow = EntryCopyMaterialToShadow.Value;
    }
}