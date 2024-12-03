using ABI_RC.Systems.ModNetwork;

namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Mod Network Internals
    
    private static bool _isSubscribedToModNetwork;
    
    internal static void Initialize()
    {
        // Packs the share bubble data a bit, also makes things just nicer
        ShareBubbleData.AddConverterForModNetwork();
    }

    internal static void Subscribe()
    {
        ModNetworkManager.Subscribe(ModId, HandleMessageReceived);
        
        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (!_isSubscribedToModNetwork) ShareBubblesMod.Logger.Error("Failed to subscribe to Mod Network! This should not happen.");
        else ShareBubblesMod.Logger.Msg("Subscribed to Mod Network.");
    }

    internal static void Unsubscribe()
    {
        ModNetworkManager.Unsubscribe(ModId);
        
        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (_isSubscribedToModNetwork) ShareBubblesMod.Logger.Error("Failed to unsubscribe from Mod Network! This should not happen.");
        else ShareBubblesMod.Logger.Msg("Unsubscribed from Mod Network.");
    }

    #endregion Mod Network Internals
}