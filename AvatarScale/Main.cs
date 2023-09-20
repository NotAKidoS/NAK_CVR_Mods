using MelonLoader;
using NAK.AvatarScaleMod.Networking;

namespace NAK.AvatarScaleMod;

public class AvatarScaleMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.PuppetMasterPatches));
        ApplyPatches(typeof(HarmonyPatches.GesturePlaneTestPatches));
        
        ModNetwork.Subscribe();
        ModSettings.InitializeModSettings();
    }

    public override void OnUpdate()
    {
        ModNetwork.Update();
        ModNetworkDebugger.DoDebugInput();
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