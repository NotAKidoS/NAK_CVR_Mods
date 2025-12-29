namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Constants

    internal const int MaxTextureSize = 1024 * 256; // 256KB
    
    internal const string NetworkVersion = "1.0.3"; // change each time network protocol changes
    private const string ModId = $"MelonMod.NAK.Stickers_v{NetworkVersion}";
    private const int ChunkSize = 1024; // roughly 1KB per ModNetworkMessage
    private const int MaxChunkCount = MaxTextureSize / ChunkSize;
    
    #endregion Constants
}