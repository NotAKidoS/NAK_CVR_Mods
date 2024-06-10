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
        
        // Experimental pickup in chair hack
        ApplyPatches(typeof(CVRPickupObjectPatches));
        
        // Experimental no interpolation on Better Better Character Controller
        ApplyPatches(typeof(BetterBetterCharacterControllerPatches));
        
        // Send relative sync update after network root data update
        ApplyPatches(typeof(NetworkRootDataUpdatePatches));
        
        // Add components if missing (for relative sync monitor and controller)
        ApplyPatches(typeof(PlayerSetupPatches));
        ApplyPatches(typeof(PuppetMasterPatches));
        
        // Add components if missing (for relative sync markers)
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