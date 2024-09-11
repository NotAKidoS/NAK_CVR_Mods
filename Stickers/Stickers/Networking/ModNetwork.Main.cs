using ABI_RC.Systems.ModNetwork;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Mod Network Internals
    
    // private static bool _isEnabled = true;
    //
    // public static bool IsEnabled
    // {
    //     get => _isEnabled;
    //     set
    //     {
    //         if (_isEnabled == value) 
    //             return;
    //         
    //         _isEnabled = value;
    //         
    //         if (_isEnabled) Subscribe();
    //         else Unsubscribe();
    //         
    //         Reset(); // reset buffers and metadata
    //     }
    // }
    
    public static bool IsSendingTexture { get; private set; }
    private static bool _isSubscribedToModNetwork;

    internal static void Subscribe()
    {
        ModNetworkManager.Subscribe(ModId, HandleMessageReceived);
        
        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (!_isSubscribedToModNetwork) StickerMod.Logger.Error("Failed to subscribe to Mod Network! This should not happen.");
        else StickerMod.Logger.Msg("Subscribed to Mod Network.");
    }

    private static void Unsubscribe()
    {
        ModNetworkManager.Unsubscribe(ModId);
        
        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (_isSubscribedToModNetwork) StickerMod.Logger.Error("Failed to unsubscribe from Mod Network! This should not happen.");
        else StickerMod.Logger.Msg("Unsubscribed from Mod Network.");
    }

    #endregion Mod Network Internals

    #region Disallow For Session
    
    private static readonly HashSet<string> _disallowedForSession = new();
    
    public static bool IsPlayerACriminal(string playerID)
    {
        return _disallowedForSession.Contains(playerID);
    }
    
    public static void HandleDisallowForSession(string playerID, bool isOn)
    {
        if (string.IsNullOrEmpty(playerID)) return;
        
        if (isOn)
        {
            _disallowedForSession.Add(playerID);
            StickerMod.Logger.Msg($"Player {playerID} has been disallowed from using stickers for this session.");
        }
        else
        {
            _disallowedForSession.Remove(playerID);
            StickerMod.Logger.Msg($"Player {playerID} has been allowed to use stickers again.");
        }
    }

    #endregion Disallow For Session
}