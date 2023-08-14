using Kafe.ChatBox;

namespace NAK.ChatBoxExtensions.Integrations;

internal class ChatBoxCommands : CommandBase
{
    public static void RegisterCommands()
    {
        bool awaitingPing = false;
        DateTime pingTime = DateTime.MinValue; // store the time when "ping" command was sent

        Commands.RegisterCommand("ping",
        onCommandSent: (message, sound, displayMsg) =>
        {
            pingTime = DateTime.Now;
            awaitingPing = true;
        },
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForSelf(message, args =>
            {
                API.SendMessage("/pong " + GetPlayerUsername(sender), false, true, true);
            });
        });

        Commands.RegisterCommand("pong",
        onCommandSent: null,
        onCommandReceived: (sender, message, sound, displayMsg) =>
        {
            RemoteCommandListenForSelf(message, args =>
            {
                if (awaitingPing)
                {
                    awaitingPing = false;
                    TimeSpan timeSincePing = DateTime.Now - pingTime; // calculate the time difference
                    API.SendMessage($"Time since ping: {timeSincePing.TotalMilliseconds}ms", false, true, true);
                    return;
                }
                API.SendMessage($"You have to ping first, {GetPlayerUsername(sender)}!", false, true, true);
            });
        });
    }
}
