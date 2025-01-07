using MelonLoader;
using NAK.RCCVirtualSteeringWheel.Patches;

namespace NAK.RCCVirtualSteeringWheel;

public class RCCVirtualSteeringWheelMod : MelonMod
{
    private static MelonLogger.Instance Logger;
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ApplyPatches(typeof(RCCCarControllerV3_Patches));
        ApplyPatches(typeof(CVRInputManager_Patches));
        
        Logger.Msg(ModSettings.EntryCustomSteeringRange);
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