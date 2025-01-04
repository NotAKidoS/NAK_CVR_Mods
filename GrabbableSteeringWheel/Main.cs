using MelonLoader;
using NAK.GrabbableSteeringWheel.Patches;

namespace NAK.GrabbableSteeringWheel;

public class GrabbableSteeringWheelMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    #region Melon Preferences
    
    // private static readonly MelonPreferences_Category Category =
    //     MelonPreferences.CreateCategory(nameof(GrabbableSteeringWheelMod));
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
        
        ApplyPatches(typeof(RCCCarControllerV3_Patches));
        ApplyPatches(typeof(CVRInputManager_Patches));
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