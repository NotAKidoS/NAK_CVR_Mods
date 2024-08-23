using ABI_RC.Core.Networking;
using ABI_RC.Systems.ModNetwork;
using DarkRift;
using NAK.Stickers.Properties;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers.Networking
{
    public static class ModNetwork
    {
        #region Configuration

        private static bool Debug_NetworkInbound => ModSettings.Debug_NetworkInbound.Value;
        private static bool Debug_NetworkOutbound => ModSettings.Debug_NetworkOutbound.Value;

        #endregion Configuration
        
        #region Constants

        internal const int MaxTextureSize = 1024 * 256; // 256KB
        
        private const string ModId = $"MelonMod.NAK.Stickers_v{AssemblyInfoParams.Version}";
        private const int ChunkSize = 1024; // roughly 1KB per ModNetworkMessage
        private const int MaxChunkCount = MaxTextureSize / ChunkSize;   
        
        #endregion Constants

        #region Enums

        private enum MessageType : byte
        {
            PlaceSticker = 0,
            ClearStickers = 1,
            StartTexture = 2,
            SendTexture = 3,
            EndTexture = 4,
            RequestTexture = 5
        }

        #endregion Enums

        #region Mod Network Internals

        internal static Action<bool> OnTextureOutboundStateChanged;

        internal static bool IsSendingTexture { get; private set; }
        private static bool _isSubscribedToModNetwork;
        
        private static byte[] _ourTextureData;
        private static (Guid hashGuid, int Width, int Height) _ourTextureMetadata;

        private static readonly Dictionary<string, byte[]> _textureChunkBuffers = new();
        private static readonly Dictionary<string, int> _receivedChunkCounts = new();
        private static readonly Dictionary<string, int> _expectedChunkCounts = new();
        private static readonly Dictionary<string, (Guid Hash, int Width, int Height)> _textureMetadata = new();

        internal static void Subscribe()
        {
            ModNetworkManager.Subscribe(ModId, OnMessageReceived);

            _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
            if (!_isSubscribedToModNetwork)
                StickerMod.Logger.Error("Failed to subscribe to Mod Network!");
        }

        public static void PlaceSticker(Vector3 position, Vector3 forward, Vector3 up)
        {
            if (!_isSubscribedToModNetwork)
                return;
            
            SendStickerPlace(_ourTextureMetadata.hashGuid, position, forward, up);
        }

        public static void ClearStickers()
        {
            if (!_isSubscribedToModNetwork)
                return;

            SendMessage(MessageType.ClearStickers);
        }

        public static bool SetTexture(byte[] imageBytes)
        {
            if (imageBytes == null 
                || imageBytes.Length == 0
                || imageBytes.Length > MaxTextureSize)
                return false;

            (Guid hashGuid, int width, int height) = ImageUtility.ExtractImageInfo(imageBytes);
            if (_ourTextureMetadata.hashGuid == hashGuid)
            {
                StickerMod.Logger.Msg($"[ModNetwork] Texture data is the same as the current texture: {hashGuid}");
                return false;
            }
            
            _ourTextureData = imageBytes;
            _ourTextureMetadata = (hashGuid, width, height);
            StickerMod.Logger.Msg($"[ModNetwork] Set texture metadata for networking: {hashGuid} ({width}x{height})");
            return true;
        }

        public static void SendTexture()
        {
            if (!IsConnectedToGameNetwork())
                return;
            
            if (!_isSubscribedToModNetwork || IsSendingTexture)
                return;
            
            if (_ourTextureData == null
                || _ourTextureMetadata.hashGuid == Guid.Empty)
                return; // no texture to send
            
            IsSendingTexture = true;
            
            // Send each chunk of the texture data
            Task.Run(() =>
            {
                try
                {
                    if (Debug_NetworkOutbound) 
                        StickerMod.Logger.Msg("[ModNetwork] Sending texture to network");
                    
                    var textureData = _ourTextureData;
                    (Guid hash, int Width, int Height) textureMetadata = _ourTextureMetadata;
                    int totalChunks = Mathf.CeilToInt(textureData.Length / (float)ChunkSize);
                    if (totalChunks > MaxChunkCount)
                    {
                        StickerMod.Logger.Error($"[ModNetwork] Texture data too large to send: {textureData.Length} bytes, {totalChunks} chunks");
                        return;
                    }

                    if (Debug_NetworkOutbound) 
                        StickerMod.Logger.Msg($"[ModNetwork] Texture data length: {textureData.Length}, total chunks: {totalChunks}, width: {textureMetadata.Width}, height: {textureMetadata.Height}");
                    
                    SendStartTexture(textureMetadata.hash, totalChunks, textureMetadata.Width, textureMetadata.Height);

                    for (int i = 0; i < textureData.Length; i += ChunkSize)
                    {
                        int size = Mathf.Min(ChunkSize, textureData.Length - i);
                        byte[] chunk = new byte[size];
                        Array.Copy(textureData, i, chunk, 0, size);

                        SendTextureChunk(chunk, i / ChunkSize);
                        Thread.Sleep(5);
                    }
                }
                catch (Exception e)
                {
                    if (Debug_NetworkOutbound) StickerMod.Logger.Error($"[ModNetwork] Failed to send texture to network: {e}");
                }
                finally
                {
                    IsSendingTexture = false;
                    SendMessage(MessageType.EndTexture);
                }
            });
        }

        private static void SendStickerPlace(Guid textureHash, Vector3 position, Vector3 forward, Vector3 up)
        {
            if (!IsConnectedToGameNetwork())
                return;

            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)MessageType.PlaceSticker);
            modMsg.Write(textureHash);
            modMsg.Write(position);
            modMsg.Write(forward);
            modMsg.Write(up);
            modMsg.Send();

            if (Debug_NetworkOutbound) StickerMod.Logger.Msg($"[Outbound] PlaceSticker: Hash: {textureHash}, Position: {position}, Forward: {forward}, Up: {up}");
        }

        private static void SendStartTexture(Guid hash, int chunkCount, int width, int height)
        {
            OnTextureOutboundStateChanged?.Invoke(true);
            
            if (!IsConnectedToGameNetwork())
                return;
            
            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)MessageType.StartTexture);
            modMsg.Write(hash);
            modMsg.Write(chunkCount);
            modMsg.Write(width);
            modMsg.Write(height);
            modMsg.Send();

            if (Debug_NetworkOutbound) StickerMod.Logger.Msg($"[Outbound] StartTexture sent with {chunkCount} chunks, width: {width}, height: {height}");
        }

        private static void SendTextureChunk(byte[] chunk, int chunkIdx)
        {
            if (!IsConnectedToGameNetwork())
                return;
            
            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)MessageType.SendTexture);
            modMsg.Write(chunkIdx);
            modMsg.Write(chunk);
            modMsg.Send();

            if (Debug_NetworkOutbound) StickerMod.Logger.Msg($"[Outbound] Sent texture chunk {chunkIdx + 1}");
        }

        private static void SendMessage(MessageType messageType)
        {
            OnTextureOutboundStateChanged?.Invoke(false);
            
            if (!IsConnectedToGameNetwork())
                return;

            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)messageType);
            modMsg.Send();

            if (Debug_NetworkOutbound) StickerMod.Logger.Msg($"[Outbound] MessageType: {messageType}");
        }

        private static void SendTextureRequest(string sender)
        {
            if (!IsConnectedToGameNetwork())
                return;
            
            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)MessageType.RequestTexture);
            modMsg.Write(sender);
            modMsg.Send();

            if (Debug_NetworkOutbound) StickerMod.Logger.Msg($"[Outbound] RequestTexture sent to {sender}");
        }

        private static void OnMessageReceived(ModNetworkMessage msg)
        {
            msg.Read(out byte msgTypeRaw);
            
            if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw))
                return;
            
            if (Debug_NetworkInbound)
                StickerMod.Logger.Msg($"[Inbound] Sender: {msg.Sender}, MessageType: {(MessageType)msgTypeRaw}");

            switch ((MessageType)msgTypeRaw)
            {
                case MessageType.PlaceSticker:
                    msg.Read(out Guid hash);
                    msg.Read(out Vector3 receivedPosition);
                    msg.Read(out Vector3 receivedForward);
                    msg.Read(out Vector3 receivedUp);
                    OnStickerPlaceReceived(msg.Sender, hash, receivedPosition, receivedForward, receivedUp);
                    break;

                case MessageType.ClearStickers:
                    OnStickersClearReceived(msg.Sender);
                    break;

                case MessageType.StartTexture:
                    msg.Read(out Guid startHash);
                    msg.Read(out int chunkCount);
                    msg.Read(out int width);
                    msg.Read(out int height);
                    OnStartTextureReceived(msg.Sender, startHash, chunkCount, width, height);
                    break;

                case MessageType.SendTexture:
                    msg.Read(out int chunkIdx);
                    msg.Read(out byte[] textureChunk);
                    OnTextureChunkReceived(msg.Sender, textureChunk, chunkIdx);
                    break;
                
                case MessageType.EndTexture:
                    OnEndTextureReceived(msg.Sender);
                    break;

                case MessageType.RequestTexture:
                    OnTextureRequestReceived(msg.Sender);
                    break;
                
                default:
                    if (Debug_NetworkInbound) StickerMod.Logger.Error($"[ModNetwork] Invalid message type received from: {msg.Sender}");
                    break;
            }
        }

        #endregion Mod Network Internals

        #region Private Methods

        private static bool IsConnectedToGameNetwork()
        {
            return NetworkManager.Instance != null
                   && NetworkManager.Instance.GameNetwork != null
                   && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
        }

        private static void OnStickerPlaceReceived(string sender, Guid textureHash, Vector3 position, Vector3 forward, Vector3 up)
        {
            Guid localHash = StickerSystem.Instance.GetPlayerStickerTextureHash(sender);
            if (localHash != textureHash && textureHash != Guid.Empty) SendTextureRequest(sender);
            StickerSystem.Instance.OnPlayerStickerPlace(sender, position, forward, up);
        }

        private static void OnStickersClearReceived(string sender)
        {
            StickerSystem.Instance.OnPlayerStickersClear(sender);
        }

        private static void OnStartTextureReceived(string sender, Guid hash, int chunkCount, int width, int height)
        {
            if (_textureChunkBuffers.ContainsKey(sender))
            {
                if (Debug_NetworkInbound) StickerMod.Logger.Warning($"[Inbound] Received StartTexture message from {sender} while still receiving texture data!");
                return;
            }

            Guid oldHash = StickerSystem.Instance.GetPlayerStickerTextureHash(sender);
            if (oldHash == hash)
            {
                if (Debug_NetworkInbound) 
                    StickerMod.Logger.Msg($"[Inbound] Received StartTexture message from {sender} with existing texture hash {hash}, skipping texture data.");
                return;
            }
            
            if (chunkCount > MaxChunkCount)
            {
                StickerMod.Logger.Error($"[Inbound] Received StartTexture message from {sender} with too many chunks: {chunkCount}");
                return;
            }
            
            _textureChunkBuffers[sender] = new byte[Mathf.Clamp(chunkCount * ChunkSize, 0, MaxTextureSize)];
            _receivedChunkCounts[sender] = 0;
            _expectedChunkCounts[sender] = chunkCount;
            _textureMetadata[sender] = (hash, width, height);

            if (Debug_NetworkInbound)
                StickerMod.Logger.Msg($"[Inbound] StartTexture received from {sender} with hash: {hash} chunk count: {chunkCount}, width: {width}, height: {height}");
        }

        private static void OnTextureChunkReceived(string sender, byte[] chunk, int chunkIdx)
        {
            if (!_textureChunkBuffers.TryGetValue(sender, out var buffer)) 
                return;
            
            int startIndex = chunkIdx * ChunkSize;
            Array.Copy(chunk, 0, buffer, startIndex, chunk.Length);

            _receivedChunkCounts[sender]++;
            if (_receivedChunkCounts[sender] < _expectedChunkCounts[sender]) 
                return;
            
            (Guid Hash, int Width, int Height) metadata = _textureMetadata[sender];
                
            // All chunks received, reassemble texture
            _textureChunkBuffers.Remove(sender);
            _receivedChunkCounts.Remove(sender);
            _expectedChunkCounts.Remove(sender);
            _textureMetadata.Remove(sender);
            
            // Validate image
            if (!ImageUtility.IsValidImage(buffer))
            {
                StickerMod.Logger.Error($"[Inbound] Received texture data is not a valid image from {sender}!");
                return;
            }
            
            // Validate data TODO: fix hash???????
            (Guid imageHash, int width, int height) = ImageUtility.ExtractImageInfo(buffer);
            if (metadata.Width != width 
                || metadata.Height != height)
            {
                StickerMod.Logger.Error($"[Inbound] Received texture data does not match metadata! Expected: {metadata.Hash} ({metadata.Width}x{metadata.Height}), received: {imageHash} ({width}x{height})");
                return;
            }
            
            Texture2D texture = new(1,1);
            texture.LoadImage(buffer);
            texture.Compress(true);

            StickerSystem.Instance.OnPlayerStickerTextureReceived(sender, metadata.Hash, texture);
            
            if (Debug_NetworkInbound) StickerMod.Logger.Msg($"[Inbound] All chunks received and texture reassembled from {sender}. " +
                                                             $"Texture size: {metadata.Width}x{metadata.Height}");
        }
        
        private static void OnEndTextureReceived(string sender)
        {
            if (!_textureChunkBuffers.ContainsKey(sender)) 
                return;
            
            if (Debug_NetworkInbound) StickerMod.Logger.Error($"[Inbound] Received EndTexture message without all chunks received from {sender}! Only {_receivedChunkCounts[sender]} out of {_expectedChunkCounts[sender]} received.");
            
            _textureChunkBuffers.Remove(sender);
            _receivedChunkCounts.Remove(sender);
            _expectedChunkCounts.Remove(sender);
            _textureMetadata.Remove(sender);
        }

        private static void OnTextureRequestReceived(string sender)
        {
            if (!_isSubscribedToModNetwork || IsSendingTexture)
                return;

            if (_ourTextureData != null && _ourTextureMetadata.hashGuid != Guid.Empty)
                SendTexture();
            else
                StickerMod.Logger.Warning($"[Inbound] Received texture request from {sender}, but no texture is set!");
        }

        #endregion Private Methods
    }
}