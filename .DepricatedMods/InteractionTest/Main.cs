using MelonLoader;

namespace NAK.InteractionTest;

public class InteractionTest : MelonMod
{
    internal static MelonLogger.Instance Logger;
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.PuppetMasterPatches));
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
    }

    void ApplyPatches(Type type)
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