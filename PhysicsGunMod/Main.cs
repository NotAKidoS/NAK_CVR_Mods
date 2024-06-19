using ABI_RC.Core.Util.AssetFiltering;
using MelonLoader;
using NAK.PhysicsGunMod.Components;
using NAK.PhysicsGunMod.HarmonyPatches;

namespace NAK.PhysicsGunMod;

public class PhysicsGunMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        // // add to prop whitelist
        // //SharedFilter._spawnableWhitelist.Add(typeof(PhysicsGunInteractionBehavior));
        //
        // // add to event whitelist
        // SharedFilter._allowedEventComponents.Add(typeof(PhysicsGunInteractionBehavior));
        // SharedFilter._allowedEventFunctions.Add(typeof(PhysicsGunInteractionBehavior), new List<string>
        // {
        //     "set_enabled",
        //     // TODO: expose more methods like release ?
        // });
        
        // apply patches
        ApplyPatches(typeof(CVRInputManagerPatches));
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