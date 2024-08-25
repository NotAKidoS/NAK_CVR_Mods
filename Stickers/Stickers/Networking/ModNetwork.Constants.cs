using NAK.Stickers.Properties;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Constants

    internal const int MaxTextureSize = 1024 * 256; // 256KB

    private const string ModId = $"MelonMod.NAK.Stickers_v{AssemblyInfoParams.Version}";
    private const int ChunkSize = 1024; // roughly 1KB per ModNetworkMessage
    private const int MaxChunkCount = MaxTextureSize / ChunkSize;

    #endregion Constants
}