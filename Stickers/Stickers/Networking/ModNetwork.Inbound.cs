using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.ModNetwork;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Inbound Buffers

    private static readonly Dictionary<string, byte[]> _textureChunkBuffers = new();
    private static readonly Dictionary<string, int> _receivedChunkCounts = new();
    private static readonly Dictionary<string, int> _expectedChunkCounts = new();
    private static readonly Dictionary<string, (int stickerSlot, Guid Hash, int Width, int Height)> _textureMetadata = new();
    
    #endregion Inbound Buffers
    
    #region Reset Method

    public static void Reset()
    {
        _textureChunkBuffers.Clear();
        _receivedChunkCounts.Clear();
        _expectedChunkCounts.Clear();
        _textureMetadata.Clear();

        LoggerInbound("ModNetwork inbound buffers and metadata have been reset.");
    }

    #endregion Reset Method

    #region Inbound Methods
    
    private static bool ShouldReceiveFromSender(string sender)
    {
        if (_disallowedForSession.Contains(sender))
            return false; // ignore messages from disallowed users

        if (MetaPort.Instance.blockedUserIds.Contains(sender))
            return false; // ignore messages from blocked users
        
        if (ModSettings.Entry_FriendsOnly.Value && !Friends.FriendsWith(sender))
            return false; // ignore messages from non-friends if friends only is enabled
        
        return true;
    }

    private static void HandleMessageReceived(ModNetworkMessage msg)
    {
        try
        {
            string sender = msg.Sender;
            msg.Read(out byte msgTypeRaw);

            if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw))
                return;
            
            if (!ShouldReceiveFromSender(sender))
                return;

            LoggerInbound($"Received message from {msg.Sender}, Type: {(MessageType)msgTypeRaw}");

            switch ((MessageType)msgTypeRaw)
            {
                case MessageType.PlaceSticker:
                    HandlePlaceSticker(msg);
                    break;
                case MessageType.ClearSticker:
                    HandleClearSticker(msg);
                    break;
                case MessageType.ClearAllStickers:
                    HandleClearAllStickers(msg);
                    break;
                case MessageType.StartTexture:
                    HandleStartTexture(msg);
                    break;
                case MessageType.SendTexture:
                    HandleSendTexture(msg);
                    break;
                case MessageType.EndTexture:
                    HandleEndTexture(msg);
                    break;
                case MessageType.RequestTexture:
                    HandleRequestTexture(msg);
                    break;
                default:
                    LoggerInbound($"Invalid message type received: {msgTypeRaw}");
                    break;
            }
        }
        catch (Exception e)
        {
            LoggerInbound($"Error handling message from {msg.Sender}: {e.Message}", true);
        }
    }

    private static void HandlePlaceSticker(ModNetworkMessage msg)
    {
        msg.Read(out int stickerSlot);
        msg.Read(out Guid textureHash);
        msg.Read(out Vector3 position);
        msg.Read(out Vector3 forward);
        msg.Read(out Vector3 up);

        if (!StickerSystem.Instance.HasTextureHash(msg.Sender, textureHash))
            SendRequestTexture(stickerSlot, textureHash);

        StickerSystem.Instance.OnStickerPlaceReceived(msg.Sender, stickerSlot, position, forward, up);
    }

    private static void HandleClearSticker(ModNetworkMessage msg)
    {
        msg.Read(out int stickerSlot);
        StickerSystem.Instance.OnStickerClearReceived(msg.Sender, stickerSlot);
    }

    private static void HandleClearAllStickers(ModNetworkMessage msg)
    {
        StickerSystem.Instance.OnStickerClearAllReceived(msg.Sender);
    }

    private static void HandleStartTexture(ModNetworkMessage msg)
    {
        string sender = msg.Sender;
        msg.Read(out int stickerSlot);
        msg.Read(out Guid textureHash);
        msg.Read(out int chunkCount);
        msg.Read(out int width);
        msg.Read(out int height);

        if (_textureChunkBuffers.ContainsKey(sender))
        {
            LoggerInbound($"Received StartTexture message from {sender} while still receiving texture data!");
            return;
        }

        if (StickerSystem.Instance.HasTextureHash(sender, textureHash))
        {
            LoggerInbound($"Received StartTexture message from {sender} with existing texture hash {textureHash}, skipping texture data.");
            return;
        }

        if (chunkCount > MaxChunkCount)
        {
            LoggerInbound($"Received StartTexture message from {sender} with too many chunks: {chunkCount}", true);
            return;
        }

        _textureMetadata[sender] = (stickerSlot, textureHash, width, height);
        _textureChunkBuffers[sender] = new byte[Mathf.Clamp(chunkCount * ChunkSize, 0, MaxTextureSize)];
        _expectedChunkCounts[sender] = Mathf.Clamp(chunkCount, 0, MaxChunkCount);
        _receivedChunkCounts[sender] = 0;
        
        LoggerInbound($"Received StartTexture message from {sender}: Slot: {stickerSlot}, Hash: {textureHash}, Chunks: {chunkCount}, Resolution: {width}x{height}");
    }

    private static void HandleSendTexture(ModNetworkMessage msg)
    {
        string sender = msg.Sender;
        msg.Read(out int chunkIdx);
        msg.Read(out byte[] chunkData);

        if (!_textureChunkBuffers.TryGetValue(sender, out var buffer))
            return;

        int startIndex = chunkIdx * ChunkSize;
        Array.Copy(chunkData, 0, buffer, startIndex, chunkData.Length);

        _receivedChunkCounts[sender]++;
        if (_receivedChunkCounts[sender] < _expectedChunkCounts[sender])
            return;

        (int stickerSlot, Guid Hash, int Width, int Height) metadata = _textureMetadata[sender];

        // All chunks received, reassemble texture
        _textureChunkBuffers.Remove(sender);
        _receivedChunkCounts.Remove(sender);
        _expectedChunkCounts.Remove(sender);
        _textureMetadata.Remove(sender);

        // Validate image
        if (!ImageUtility.IsValidImage(buffer))
        {
            LoggerInbound($"[Inbound] Received texture data is not a valid image from {sender}!", true);
            return;
        }

        // Validate data TODO: fix hash???????
        (Guid imageHash, int width, int height) = ImageUtility.ExtractImageInfo(buffer);
        if (metadata.Width != width
            || metadata.Height != height)
        {
            LoggerInbound($"Received texture data does not match metadata! Expected: {metadata.Hash} ({metadata.Width}x{metadata.Height}), received: {imageHash} ({width}x{height})", true);
            return;
        }

        Texture2D texture = new(1,1);
        texture.LoadImage(buffer);
        texture.Compress(true);

        StickerSystem.Instance.OnPlayerStickerTextureReceived(sender, metadata.Hash, texture, metadata.stickerSlot);

        LoggerInbound($"All chunks received and texture reassembled from {sender}. " +
                      $"Texture size: {metadata.Width}x{metadata.Height}");
    }

    private static void HandleEndTexture(ModNetworkMessage msg)
    {
        string sender = msg.Sender;
        if (!_textureChunkBuffers.ContainsKey(sender))
            return;

        LoggerInbound($"Received EndTexture message without all chunks received from {sender}! Only {_receivedChunkCounts[sender]} out of {_expectedChunkCounts[sender]} received.");

        _textureChunkBuffers.Remove(sender);
        _receivedChunkCounts.Remove(sender);
        _expectedChunkCounts.Remove(sender);
        _textureMetadata.Remove(sender);
    }

    private static void HandleRequestTexture(ModNetworkMessage msg)
    {
        string sender = msg.Sender;
        msg.Read(out int stickerSlot);
        msg.Read(out Guid textureHash);

        if (!_isSubscribedToModNetwork || IsSendingTexture)
            return;

        if (stickerSlot < 0 || stickerSlot >= _textureStorage.Length)
        {
            LoggerInbound($"Received RequestTexture message from {sender} with invalid slot {stickerSlot}!");
            return;
        }
        
        if (_textureStorage[stickerSlot].textureHash != textureHash)
        {
            LoggerInbound($"Received RequestTexture message from {sender} with invalid texture hash {textureHash} for slot {stickerSlot}!");
            return;
        }
        
        SendTexture(stickerSlot);
    }

    #endregion Inbound Methods
}