using MelonLoader;
using NAK.RelativeSync.Networking;
using NAK.RelativeSync.Patches;

namespace NAK.RelativeSync;

public class RelativeSyncMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ModNetwork.Subscribe();
        
        ApplyPatches(typeof(NetworkRootDataUpdatePatches));
        ApplyPatches(typeof(CVRSpawnablePatches));
        
        ApplyPatches(typeof(PlayerSetupPatches));
        ApplyPatches(typeof(PuppetMasterPatches));
        
        ApplyPatches(typeof(CVRSeatPatches));
        ApplyPatches(typeof(CVRMovementParentPatches));
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
}