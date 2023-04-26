using Kafe.ChatBox;

namespace NAK.Melons.ChatBoxExtensions.Integrations;

internal class ChatBoxCommands : CommandBase
{
    public static void RegisterCommands()
    {
        bool awaitingPing = false;
        DateTime pingTime = DateTime.MinValue; // store the time when "ping" command was sent

        Commands.RegisterCommand("ping",
        onCommandSent: (message, sound) =>
        {
            pingTime = DateTime.Now;
            awaitingPing = true;
        },
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForSelf(message, args =>
            {
                ChatBox.SendMessage("/pong " + GetPlayerUsername(sender), false);
            });
        });

        Commands.RegisterCommand("pong",
        onCommandSent: null,
        onCommandReceived: (sender, message, sound) =>
        {
            RemoteCommandListenForSelf(message, args =>
            {
                if (awaitingPing)
                {
                    awaitingPing = false;
                    TimeSpan timeSincePing = DateTime.Now - pingTime; // calculate the time difference
                    ChatBox.SendMessage($"Time since ping: {timeSincePing.TotalMilliseconds}ms", false);
                    return;
                }
                ChatBox.SendMessage($"You have to ping first, {GetPlayerUsername(sender)}!", false);
            });
        });
    }
}