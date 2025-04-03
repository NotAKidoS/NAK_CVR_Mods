using System;
using MelonLoader;

namespace NAK.VisualCloneFix;

public class VisualCloneFixMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(VisualCloneFix));

    internal static readonly MelonPreferences_Entry<bool> EntryUseVisualClone =
        Category.CreateEntry("use_visual_clone", true,
            "Use Visual Clone", description: "Uses the potentially faster Visual Clone setup for the local avatar.");
    
    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(Patches)); // slapped together a fix cause HarmonyInstance.Patch was null ref for no reason?
    }
    
    #endregion Melon Events

    #region Melon Mod Utilities

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
    
    #endregion Melon Mod Utilities
}