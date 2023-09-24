using MelonLoader;
using NAK.AvatarScaleMod.InputHandling;
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
        
        InitializeIntegration("BTKUILib", Integrations.BTKUIAddon.Initialize);
        
        ModNetwork.Subscribe();
        ModSettings.InitializeModSettings();
    }

    public override void OnUpdate()
    {
        ModNetwork.Update();
        DebugKeybinds.DoDebugInput();
    }
    
    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
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