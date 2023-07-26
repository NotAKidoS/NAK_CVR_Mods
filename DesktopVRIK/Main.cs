using MelonLoader;

namespace NAK.DesktopVRIK;

public class DesktopVRIK : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));

        InitializeIntegration("BTKUILib", Integrations.BTKUIAddon.Initialize);
        InitializeIntegration("AvatarMotionTweaker", Integrations.AMTAddon.Initialize);
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