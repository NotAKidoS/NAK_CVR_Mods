using MelonLoader;
using NAK.ReconnectionSystemFix.Patches;

namespace NAK.ReconnectionSystemFix;

public class ReconnectionSystemFix : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(NetworkManagerPatches));
        ApplyPatches(typeof(AvatarUpdatePatches));
        ApplyPatches(typeof(CVRPlayerManagerPatches));
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