using ABI_RC.Core.Networking;
using ABI_RC.Core.Player;
using ABI_RC.Systems.ModNetwork;

namespace NAK.CleanPlates.Network;

public static class CleanPlatesNetwork
{
    private const string NetworkVersion = "1.1.0"; // change each time network protocol changes
    private const string ModId = $"NAK.CleanPlates:{NetworkVersion}"; // Cannot exceed 32 characters

    private static bool _isSubscribedToModNetwork;

    public static void Init()
    {
        ModNetworkManager.Subscribe(ModId, HandleMessageReceived);

        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (!_isSubscribedToModNetwork) CleanPlatesMod.Logger.Error("Failed to subscribe to Mod Network! This should not happen.");
        else CleanPlatesMod.Logger.Msg("Subscribed to Mod Network.");
    }

    private enum MessageType
    {
        SignalTTS,
        ProfileUpdate,
    }

    private static void HandleMessageReceived(ModNetworkMessage msg)
    {
        try
        {
            string sender = msg.Sender;
            msg.Read(out byte msgTypeRaw);

            LoggerInbound($"Received message from {msg.Sender}, Type: {(MessageType)msgTypeRaw}");

            switch ((MessageType)msgTypeRaw)
            {
                case MessageType.SignalTTS:
                    HandleSignalTTS(sender, msg);
                    break;
                case MessageType.ProfileUpdate:
                    PlateManager.RefreshProfile(sender);
                    break;
            }
        }
        catch (Exception e)
        {
            LoggerInbound($"Error handling message from {msg.Sender}: {e.Message}", MNLogLevel.Warning);
        }
    }

    // Call after changing your own pfp or pronouns, everyone else refetches.
    public static void SendProfileUpdate()
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.ProfileUpdate);
        modMsg.Send();

        LoggerOutbound("Sending ProfileUpdate message");
    }

    private static void HandleSignalTTS(string sender, ModNetworkMessage msg)
    {
        msg.Read(out float seconds);
        PlateManager.SetPlayingTts(sender, seconds);
    }
    
    public static void SendSignalTTS(float seconds)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.SignalTTS);
        modMsg.Write(seconds);
        modMsg.Send();

        LoggerOutbound($"Sending SignalTTS message: {seconds:F2}s");
    }

    #region Utility

    private static bool CanSendModNetworkMessage()
        => _isSubscribedToModNetwork
           && NetworkManager.Instance.IsConnectedToGameNetwork()
           && CVRPlayerManager.Instance.NetworkPlayers.Count > 0; // No need to send if there are no players

    #endregion Utility

    #region Network Logging

    private enum MNLogLevel : byte { Info, Warning, Error }

    private static void LoggerInbound(string message, MNLogLevel type = MNLogLevel.Info)
        => _logger($"[Inbound] {message}", type, CleanPlatesMod.Debug_NetworkInbound.Value);

    private static void LoggerOutbound(string message, MNLogLevel type = MNLogLevel.Info)
        => _logger($"[Outbound] {message}", type, CleanPlatesMod.Debug_NetworkOutbound.Value);

    private static void _logger(string message, MNLogLevel type = MNLogLevel.Info, bool loggerSetting = true)
    {
        // Errors are always logged, regardless of setting.
        if (type != MNLogLevel.Error && !loggerSetting) return;

        switch (type)
        {
            case MNLogLevel.Warning:
                CleanPlatesMod.Logger.Warning(message);
                break;
            case MNLogLevel.Error:
                CleanPlatesMod.Logger.Error(message);
                break;
            default:
                CleanPlatesMod.Logger.Msg(message);
                break;
        }
    }

    #endregion Network Logging
}