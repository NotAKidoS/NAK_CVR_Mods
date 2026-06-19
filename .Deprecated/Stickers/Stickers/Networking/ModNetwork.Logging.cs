namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Network Logging

    private static void LoggerInbound(string message, bool isWarning = false)
    {
        if (!ModSettings.Debug_NetworkInbound.Value) return;
        if (isWarning) StickerMod.Logger.Warning("[Inbound] " + message);
        else StickerMod.Logger.Msg("[Inbound] " + message);
    }

    private static void LoggerOutbound(string message, bool isWarning = false)
    {
        if (!ModSettings.Debug_NetworkOutbound.Value) return;
        if (isWarning) StickerMod.Logger.Warning("[Outbound] " + message);
        else StickerMod.Logger.Msg("[Outbound] " + message);
    }

    #endregion Network Logging
}