using ABI_RC.Systems.ModNetwork;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Texture Data
    
    private static readonly (byte[] textureData, Guid textureHash, int width, int height)[] _textureStorage = new (byte[], Guid, int, int)[ModSettings.MaxStickerSlots];
    
    public static bool SetTexture(int stickerSlot, byte[] imageBytes)
    {
        if (imageBytes == null 
            || imageBytes.Length == 0
            || imageBytes.Length > MaxTextureSize)
            return false;

        (Guid hashGuid, int width, int height) = ImageUtility.ExtractImageInfo(imageBytes);
        if (_textureStorage[stickerSlot].textureHash == hashGuid)
        {
            LoggerOutbound($"Texture data is the same as the current texture for slot {stickerSlot}: {hashGuid}");
            return false;
        }

        _textureStorage[stickerSlot] = (imageBytes, hashGuid, width, height);
        LoggerOutbound($"Set texture data for slot {stickerSlot}, metadata: {hashGuid} ({width}x{height})");
        
        SendTexture(stickerSlot);
        
        return true;
    }

    #endregion Texture Data
    
    #region Outbound Methods

    public static void SendPlaceSticker(int stickerSlot, Vector3 position, Vector3 forward, Vector3 up)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.PlaceSticker);
        modMsg.Write(stickerSlot);
        modMsg.Write(_textureStorage[stickerSlot].textureHash);
        modMsg.Write(position);
        modMsg.Write(forward);
        modMsg.Write(up);
        modMsg.Send();

        LoggerOutbound($"PlaceSticker: Slot: {stickerSlot}, Hash: {_textureStorage[stickerSlot].textureHash}, Position: {position}, Forward: {forward}, Up: {up}");
    }

    public static void SendClearSticker(int stickerSlot)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.ClearSticker);
        modMsg.Write(stickerSlot);
        modMsg.Send();

        LoggerOutbound($"ClearSticker: Slot: {stickerSlot}");
    }

    public static void SendClearAllStickers()
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.ClearAllStickers);
        modMsg.Send();

        LoggerOutbound("ClearAllStickers");
    }

    private static void SendStartTexture(int stickerSlot, Guid textureHash, int chunkCount, int width, int height)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.StartTexture);
        modMsg.Write(stickerSlot);
        modMsg.Write(textureHash);
        modMsg.Write(chunkCount);
        modMsg.Write(width);
        modMsg.Write(height);
        modMsg.Send();

        LoggerOutbound($"StartTexture: Slot: {stickerSlot}, Hash: {textureHash}, Chunks: {chunkCount}, Size: {width}x{height}");
    }

    public static void SendTextureChunk(int chunkIdx, byte[] chunkData)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.SendTexture);
        modMsg.Write(chunkIdx);
        modMsg.Write(chunkData);
        modMsg.Send();

        LoggerOutbound($"SendTextureChunk: Index: {chunkIdx}, Size: {chunkData.Length} bytes");
    }

    public static void SendEndTexture()
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.EndTexture);
        modMsg.Send();

        LoggerOutbound("EndTexture");
    }

    public static void SendRequestTexture(int stickerSlot, Guid textureHash)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.RequestTexture);
        modMsg.Write(stickerSlot);
        modMsg.Write(textureHash);
        modMsg.Send();

        LoggerOutbound($"RequestTexture: Slot: {stickerSlot}, Hash: {textureHash}");
    }

    public static void SendTexture(int stickerSlot)
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        if (!IsConnectedToGameNetwork() || IsSendingTexture)
            return;

        IsSendingTexture = true;

        Task.Run(() =>
        {
            try
            {
                Thread.CurrentThread.IsBackground = false; // working around bug in MTJobManager
                
                var textureData = _textureStorage[stickerSlot].textureData;
                var textureHash = _textureStorage[stickerSlot].textureHash;
                var width = _textureStorage[stickerSlot].width;
                var height = _textureStorage[stickerSlot].height;
                int totalChunks = Mathf.CeilToInt(textureData.Length / (float)ChunkSize);
                if (totalChunks > MaxChunkCount)
                {
                    LoggerOutbound($"Texture data too large to send for slot {stickerSlot}: {textureData.Length} bytes, {totalChunks} chunks", true);
                    return;
                }

                LoggerOutbound($"Sending texture for slot {stickerSlot}: {textureData.Length} bytes, Chunks: {totalChunks}, Resolution: {width}x{height}");

                SendStartTexture(stickerSlot, textureHash, totalChunks, width, height);

                for (int i = 0; i < textureData.Length; i += ChunkSize)
                {
                    int size = Mathf.Min(ChunkSize, textureData.Length - i);
                    byte[] chunk = new byte[size];
                    Array.Copy(textureData, i, chunk, 0, size);

                    SendTextureChunk(i / ChunkSize, chunk);
                    Thread.Sleep(5); // Simulate network latency
                }

                SendEndTexture();
            }
            catch (Exception e)
            {
                LoggerOutbound($"Failed to send texture for slot {stickerSlot}: {e}", true);
            }
            finally
            {
                IsSendingTexture = false;
                InvokeTextureOutboundStateChanged(false);
                Thread.CurrentThread.IsBackground = true; // working around bug in MTJobManager
            }
        });
    }

    #endregion Outbound Methods
}