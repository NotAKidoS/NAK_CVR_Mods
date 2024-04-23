namespace NAK.ChatBoxExtensions.Integrations;

public static class Commands
{

    private const string Character = "/";
    private static readonly List<Command> CommandList = new();

    internal static void InitializeCommandHandlers()
    {
        Kafe.ChatBox.API.OnMessageSent += msg => HandleSentCommand(msg.Message, msg.TriggerNotification, msg.DisplayOnChatBox);
        Kafe.ChatBox.API.OnMessageReceived += msg => HandleReceivedCommand(msg.SenderGuid, msg.Message, msg.TriggerNotification, msg.DisplayOnChatBox);
    }

    internal static void RegisterCommand(string prefix, Action<string, bool, bool> onCommandSent = null, Action<string, string, bool, bool> onCommandReceived = null)
    {
        var cmd = new Command { Prefix = prefix, OnCommandSent = onCommandSent, OnCommandReceived = onCommandReceived };
        CommandList.Add(cmd);
    }

    internal static void UnregisterCommand(string prefix)
    {
        CommandList.RemoveAll(cmd => cmd.Prefix == prefix);
    }

    private class Command
    {
        internal string Prefix;

        // Command Sent (message)
        internal Action<string, bool, bool> OnCommandSent;

        // Command Sent (sender guid, message)
        internal Action<string, string, bool, bool> OnCommandReceived;
    }

    private static void HandleSentCommand(string message, bool notification, bool displayMsg)
    {
        if (!message.StartsWith(Character)) return;
        foreach (var command in CommandList.Where(command => message.StartsWith(Character + command.Prefix)))
        {
            command.OnCommandSent?.Invoke(message, notification, displayMsg);
        }
    }

    private static void HandleReceivedCommand(string sender, string message, bool notification, bool displayMsg)
    {
        if (!message.StartsWith(Character)) return;
        foreach (var command in CommandList.Where(command => message.StartsWith(Character + command.Prefix)))
        {
            command.OnCommandReceived?.Invoke(sender, message, notification, displayMsg);
        }
    }
}
