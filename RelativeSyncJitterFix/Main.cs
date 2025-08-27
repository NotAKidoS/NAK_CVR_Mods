using MelonLoader;

namespace NAK.RelativeSyncJitterFix;

public class RelativeSyncJitterFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Experimental sync hack
        ApplyPatches(typeof(Patches.CVRSpawnablePatches));
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