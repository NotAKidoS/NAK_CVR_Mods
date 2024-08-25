using ABI_RC.Systems.ModNetwork;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Mod Network Internals
    
    public static bool IsSendingTexture { get; private set; }
    private static bool _isSubscribedToModNetwork;

    internal static void Subscribe()
    {
        ModNetworkManager.Subscribe(ModId, HandleMessageReceived);
        
        _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
        if (!_isSubscribedToModNetwork) StickerMod.Logger.Error("Failed to subscribe to Mod Network! This should not happen.");
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