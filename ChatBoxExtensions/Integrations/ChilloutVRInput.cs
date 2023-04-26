namespace NAK.Melons.ChatBoxExtensions.Integrations;

internal class ChilloutVRInputCommands : CommandBase
{
    public static void RegisterCommands()
    {
        Commands.RegisterCommand("emote",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                if (args.Length > 0 && int.TryParse(args[0], out int emote))
                {
                    ChatBoxExtensions.InputModule.emote = (float)emote;
                }
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                if (args.Length > 1 && int.TryParse(args[1], out int emote))
                {
                    ChatBoxExtensions.InputModule.emote = (float)emote;
                }
            });
        });

        Commands.RegisterCommand("jump",
        onCommandSent: (message, sound, displayMsg) =>
        {
            LocalCommandIgnoreOthers(message, args =>
            {
                if (args.Length > 0 && bool.TryParse(args[0], out bool jump))
                {
                    ChatBoxExtensions.InputModule.jump = jump;
                    return;
                }
                ChatBoxExtensions.InputModule.jump = true;
            });
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForAll(message, args =>
            {
                if (bool.TryParse(args[0], out bool jump))
                {
                    ChatBoxExtensions.InputModule.jump = jump;
                }
            });
        });
    }
}
