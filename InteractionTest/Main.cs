using MelonLoader;

namespace NAK.InteractionTest;

public class InteractionTestMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    #region Melon Mod Overrides
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ApplyPatches(typeof(Patches.ControllerRayPatches));
    }
    
    #endregion Melon Mod Overrides

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
