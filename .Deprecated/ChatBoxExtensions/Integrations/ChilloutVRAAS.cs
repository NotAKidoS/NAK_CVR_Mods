using ABI_RC.Core.Player;

namespace NAK.ChatBoxExtensions.Integrations;

internal class ChilloutVRAASCommands : CommandBase
{
    public static void RegisterCommands()
    {
        // /aas [target player] [name] [value]
        Commands.RegisterCommand("aas",
            onCommandSent: (message, sound, displayMsg) =>
            {
                LocalCommandIgnoreOthers(message, args =>
                {
                    if (args.Length > 2 && float.TryParse(args[2], out float value))
                    {
                        PlayerSetup.Instance.ChangeAnimatorParam(args[1], value);
                    }
                });
            },
            onCommandReceived: (sender, message, sound, displayMsg) =>
            {
                RemoteCommandListenForAll(message, args =>
                {
                    if (args.Length > 2 && float.TryParse(args[2], out float value))
                    {
                        PlayerSetup.Instance.ChangeAnimatorParam(args[1], value);
                    }
                });
            });
    }
}