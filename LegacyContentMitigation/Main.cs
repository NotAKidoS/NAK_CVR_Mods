using ABI_RC.Core.InteractionSystem;
using MelonLoader;
using UnityEngine;

namespace NAK.LegacyContentMitigation;

public class LegacyContentMitigationMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    #region Melon Preferences
    
    // private static readonly MelonPreferences_Category Category =
    //     MelonPreferences.CreateCategory(nameof(LegacyContentMitigationMod));
    // 
    // private static readonly MelonPreferences_Entry<bool> EntryEnabled = 
    //     Category.CreateEntry(
    //         "use_legacy_mitigation",
    //         true, 
    //         "Enabled", 
    //         description: "Enable legacy content camera hack when in Legacy worlds.");
    
    #endregion Melon Preferences
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ApplyPatches(typeof(Patches.PlayerSetup_Patches)); // add MultiPassCamera to VR camera
        ApplyPatches(typeof(Patches.SceneLoaded_Patches)); // enable / disable in legacy worlds
        ApplyPatches(typeof(Patches.CVRWorld_Patches)); // post processing shit
        ApplyPatches(typeof(Patches.CVRTools_Patches)); // prevent shader replacement when fix is active
        ApplyPatches(typeof(Patches.HeadHiderManager_Patches)); // prevent main cam triggering early head hide
        ApplyPatches(typeof(Patches.CVRMirror_Patches));
        
        InitializeIntegration("BTKUILib", Integrations.BtkUiAddon.Initialize); // quick menu options
    }
    
    #endregion Melon Events
    
    #region Melon Mod Utilities
    
    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }
    
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