using MelonLoader;
using NAK.IKAdjustments.HarmonyPatches;
using NAK.IKAdjustments.Integrations;

namespace NAK.IKAdjustments;

public class IKAdjustments : MelonMod
{
    internal static string SettingsCategory = nameof(IKAdjustments);

    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(IKSystemPatches));

        InitializeIntegration("BTKUILib", BTKUIAddon.Initialize);
    }

    private void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        LoggerInstance.Msg($"Initializing {modName} integration.");
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