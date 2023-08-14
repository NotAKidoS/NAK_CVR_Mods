using MelonLoader;

namespace NAK.AvatarScaleMod;

public class AvatarScaleMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        ModSettings.InitializeModSettings();
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        //ApplyPatches(typeof(HarmonyPatches.PuppetMasterPatches));
        ApplyPatches(typeof(HarmonyPatches.GesturePlaneTestPatches));
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