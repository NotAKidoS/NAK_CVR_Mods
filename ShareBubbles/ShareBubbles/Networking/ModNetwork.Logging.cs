namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Network Logging

    private static void LoggerInbound(string message, MNLogLevel type = MNLogLevel.Info)
        => _logger($"[Inbound] {message}", type, ModSettings.Debug_NetworkInbound.Value);

    private static void LoggerOutbound(string message, MNLogLevel type = MNLogLevel.Info)
        => _logger($"[Outbound] {message}", type, ModSettings.Debug_NetworkOutbound.Value);
    
    private static void _logger(string message, MNLogLevel type = MNLogLevel.Info, bool loggerSetting = true)
    {
        switch (type)
        {
            default:
            case MNLogLevel.Info when loggerSetting:
                ShareBubblesMod.Logger.Msg(message);
                break;
            case MNLogLevel.Warning when loggerSetting:
                ShareBubblesMod.Logger.Warning(message);
                break;
            case MNLogLevel.Error: // Error messages are always logged, regardless of setting
                ShareBubblesMod.Logger.Error(message);
                break;
                
        }
    }

    #endregion Network Logging
}