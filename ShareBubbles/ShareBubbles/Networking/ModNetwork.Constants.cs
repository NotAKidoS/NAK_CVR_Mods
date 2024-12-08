namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Constants
    
    private const string NetworkVersion = "1.0.1"; // change each time network protocol changes
    private const string ModId = $"NAK.SB:{NetworkVersion}"; // Cannot exceed 32 characters
    
    private const float ClaimRequestTimeout = 30f; // 30 second timeout
    
    #endregion Constants
}