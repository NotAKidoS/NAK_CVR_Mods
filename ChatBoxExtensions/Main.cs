using MelonLoader;
using NAK.ChatBoxExtensions.InputModules;

namespace NAK.ChatBoxExtensions;

public class ChatBoxExtensions : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal static InputModuleChatBoxExtensions InputModule;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        if (!MelonMod.RegisteredMelons.Any(it => it.Info.Name == "ChatBox"))
        {
            Logger.Error("ChatBox was not found!");
            return;
        }

        ApplyIntegrations();
    }

    void ApplyIntegrations()
    {
        Integrations.Commands.InitializeCommandHandlers();
        Integrations.ChatBoxCommands.RegisterCommands();
        Integrations.ChilloutVRBaseCommands.RegisterCommands();
        ApplyPatches(typeof(HarmonyPatches.CVRInputManagerPatches));

        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "PlayerRagdollMod"))
        {
            Integrations.PlayerRagdollModCommands.RegisterCommands();
        }
    }

    void ApplyPatches(Type type)
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
