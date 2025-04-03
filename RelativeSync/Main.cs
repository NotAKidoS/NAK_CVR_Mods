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
        ModSettings.Initialize();
        
        // Experimental sync hack
        ApplyPatches(typeof(CVRSpawnablePatches));

        // Send relative sync update after network root data update
        ApplyPatches(typeof(NetworkRootDataUpdatePatches));
        
        // Add components if missing (for relative sync monitor and controller)
        ApplyPatches(typeof(PlayerSetupPatches));
        ApplyPatches(typeof(PuppetMasterPatches));
        
        // Add components if missing (for relative sync markers)
        ApplyPatches(typeof(CVRSeatPatches));
        ApplyPatches(typeof(CVRMovementParentPatches));
        
        // So we run after the client moves the remote player
        ApplyPatches(typeof(NetIKController_Patches));
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