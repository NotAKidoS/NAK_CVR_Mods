using ABI_RC.Core;
using ABI_RC.Core.Base;
using Kafe.ChatBox;

namespace NAK.ChatBoxExtensions.Integrations;

internal class ChilloutVRBaseCommands : CommandBase
{
    public static void RegisterCommands()
    {
        Commands.RegisterCommand("respawn",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                RootLogic.Instance.Respawn();
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, (args) =>
            {
                RootLogic.Instance.Respawn();
            });
        });

        Commands.RegisterCommand("mute",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                Audio.SetMicrophoneActive(true);
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                Audio.SetMicrophoneActive(true);
            });
        });
    }
}
