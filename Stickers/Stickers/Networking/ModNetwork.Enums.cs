namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Enums

    // Remote clients will request textures from the sender if they receive PlaceSticker with a textureHash they don't have

    private enum MessageType : byte
    {
        PlaceSticker = 0,          // stickerSlot, textureHash, position, forward, up
        ClearSticker = 1,          // stickerSlot
        ClearAllStickers = 2,      // none
        StartTexture = 3,          // stickerSlot, textureHash, chunkCount, width, height
        SendTexture = 4,           // chunkIdx, chunkData
        EndTexture = 5,            // none
        RequestTexture = 6         // stickerSlot, textureHash
    }

    #endregion Enums
}