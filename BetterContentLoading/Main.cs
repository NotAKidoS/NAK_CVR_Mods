using MelonLoader;
using NAK.BetterContentLoading.Patches;

namespace NAK.BetterContentLoading;

public class BetterContentLoadingMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ApplyPatches(typeof(CVRDownloadManager_Patches));
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