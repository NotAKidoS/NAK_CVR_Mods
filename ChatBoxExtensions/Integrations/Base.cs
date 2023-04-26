using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;

namespace NAK.ChatBoxExtensions.Integrations;

public class CommandBase
{
    internal static bool IsCommandForAll(string argument)
    {
        if (String.IsNullOrWhiteSpace(argument)) return false;
        return argument == "*" || argument.StartsWith("@a") || argument.StartsWith("@e");
    }

    internal static bool IsCommandForLocalPlayer(string argument)
    {
        if (String.IsNullOrWhiteSpace(argument)) return false;
        if (argument.Contains("*"))
        {
            string partialName = argument.Replace("*", "").Trim();
            if (String.IsNullOrWhiteSpace(partialName)) return false;
            return MetaPort.Instance.username.Contains(partialName);
        }
        return MetaPort.Instance.username == argument;
    }

    internal static void LocalCommandIgnoreOthers(string argument, Action<string[]> callback)
    {
        string[] args = argument.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        // will fail if arguments are specified which arent local player
        if (args.Length == 0 || IsCommandForAll(args[0]) || IsCommandForLocalPlayer(args[0])) callback(args);
    }

    //remote must specify exact player, wont respawn to all
    internal static void RemoteCommandListenForSelf(string argument, Action<string[]> callback)
    {
        string[] args = argument.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length == 0) return;
        if (IsCommandForLocalPlayer(args[0])) callback(args);
    }

    // remote must specify player or all, ignore commands without arguments
    internal static void RemoteCommandListenForAll(string argument, Action<string[]> callback)
    {
        string[] args = argument.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length == 0) return;
        if (IsCommandForAll(args[0]) || IsCommandForLocalPlayer(args[0])) callback(args);
    }

    internal static string GetPlayerUsername(string guid)
    {
        return CVRPlayerManager.Instance.TryGetPlayerName(guid);
    }
}