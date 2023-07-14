using MelonLoader;

namespace NAK.AlternateIKSystem;

// IKManager is what the game talks to
// BodyControl is the master vrik tracking weights

// IKHandler is created by IKManager, specific to Desktop/VR
// It will look at BodyControl to manage its own vrik solver

// IKCalibrator will setup vrik & its settings

public class AlternateIKSystem : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));

        InitializeIntegration("BTKUILib", Integrations.BTKUIAddon.Initialize);
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
            Logger.Msg($"Failed while patching {type.Name}!");
            Logger.Error(e);
        }
    }
}