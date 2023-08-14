using MelonLoader;
using NAK.ChatBoxExtensions.InputModules;

namespace NAK.ChatBoxExtensions;

public class ChatBoxExtensions : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal static InputModuleChatBoxExtensions InputModule = new();

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        if (RegisteredMelons.All(it => it.Info.Name != "ChatBox"))
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
        Integrations.ChilloutVRAASCommands.RegisterCommands();
        Integrations.ChilloutVRInputCommands.RegisterCommands();

        ApplyPatches(typeof(HarmonyPatches.CVRInputManagerPatches));

        if (RegisteredMelons.Any(it => it.Info.Name == "PlayerRagdollMod"))
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
