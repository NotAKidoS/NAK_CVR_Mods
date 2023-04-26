using ABI_RC.Core;
using ABI_RC.Core.Base;
using Kafe.ChatBox;

namespace NAK.Melons.ChatBoxExtensions.Integrations;

internal class ChilloutVRBaseCommands : CommandBase
{
    public static void RegisterCommands()
    {
        Commands.RegisterCommand("respawn",
        onCommandSent: (message, sound) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                RootLogic.Instance.Respawn();
            });
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                RootLogic.Instance.Respawn();
            });
        });

        Commands.RegisterCommand("mute",
        onCommandSent: (message, sound) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                Audio.SetMicrophoneActive(true);
            });
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                Audio.SetMicrophoneActive(true);
            });
        });
    }
}