using ABI_RC.Core.Networking;
using ABI_RC.Systems.ModNetwork;
using DarkRift;
using UnityEngine;

namespace NAK.RelativeSync.Networking
{
    public static class ModNetwork
    {
        public static bool Debug_NetworkInbound = false;
        public static bool Debug_NetworkOutbound = false;

        private static bool _isSubscribedToModNetwork;

        private struct RelativeSyncData
        {
            public bool HasSyncedThisData;
            public int MarkerHash;
            public Vector3 Position;
            public Vector3 Rotation;
        }
        
        private static RelativeSyncData _latestRelativeSyncData;
        
        #region Constants

        private const string ModId = "MelonMod.NAK.RelativeSync";

        #endregion

        #region Enums

        private enum MessageType : byte
        {
            SyncPosition = 0,
            RelativeSyncStatus = 1
        }

        #endregion

        #region Mod Network Internals

        internal static void Subscribe()
        {
            ModNetworkManager.Subscribe(ModId, OnMessageReceived);

            _isSubscribedToModNetwork = ModNetworkManager.IsSubscribed(ModId);
            if (!_isSubscribedToModNetwork)
                Debug.LogError("Failed to subscribe to Mod Network!");
        }

        internal static void SendRelativeSyncUpdate()
        {
            if (!_isSubscribedToModNetwork)
                return;
            
            if (!_latestRelativeSyncData.HasSyncedThisData)
            {
                SendMessage(MessageType.SyncPosition, _latestRelativeSyncData.MarkerHash,
                    _latestRelativeSyncData.Position, _latestRelativeSyncData.Rotation);
                _latestRelativeSyncData.HasSyncedThisData = true;
            }
        }

        private static void SetLatestRelativeSync(int markerHash, Vector3 position, Vector3 rotation)
        {
            // check if the data has changed
            if (_latestRelativeSyncData.MarkerHash == markerHash
                && _latestRelativeSyncData.Position == position
                && _latestRelativeSyncData.Rotation == rotation)
                return; // no need to update
            
            _latestRelativeSyncData.HasSyncedThisData = false; // reset
            _latestRelativeSyncData.MarkerHash = markerHash;
            _latestRelativeSyncData.Position = position;
            _latestRelativeSyncData.Rotation = rotation;
        }

        private static void SendMessage(MessageType messageType, int markerHash, Vector3 position, Vector3 rotation)
        {
            if (!IsConnectedToGameNetwork())
                return;

            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)messageType);
            modMsg.Write(markerHash);
            modMsg.Write(position);
            modMsg.Write(rotation);
            modMsg.Send();
        }

        private static void OnMessageReceived(ModNetworkMessage msg)
        {
            msg.Read(out byte msgTypeRaw);

            if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw))
                return;

            switch ((MessageType)msgTypeRaw)
            {
                case MessageType.SyncPosition:
                    msg.Read(out int markerHash);
                    msg.Read(out Vector3 receivedPosition);
                    msg.Read(out Vector3 receivedRotation);
                    OnNetworkPositionUpdateReceived(msg.Sender, markerHash, receivedPosition, receivedRotation);
                    break;
                // case MessageType.RelativeSyncStatus:
                //     msg.Read(out string guidStr);
                //     msg.Read(out bool isRelativeSync);
                //     System.Guid guid = new System.Guid(guidStr);
                //     OnRelativeSyncStatusReceived(msg.Sender, guid, isRelativeSync);
                //     break;
                default:
                    Debug.LogError($"Invalid message type received from: {msg.Sender}");
                    break;
            }
        }

        #endregion

        #region Public Methods

        public static void SendNetworkPosition(int markerHash, Vector3 newPosition, Vector3 newRotation)
        {
            SetLatestRelativeSync(markerHash, newPosition, newRotation);
        }

        #endregion

        #region Private Methods

        private static bool IsConnectedToGameNetwork()
        {
            return NetworkManager.Instance != null
                   && NetworkManager.Instance.GameNetwork != null
                   && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
        }

        private static void OnNetworkPositionUpdateReceived(string sender, int markerHash, Vector3 position, Vector3 rotation)
        {
            RelativeSyncManager.ApplyRelativeSync(sender, markerHash, position, rotation);
        }

        private static void OnRelativeSyncStatusReceived(string sender, System.Guid guid, bool isRelativeSync)
        {
            // todo: implement
        }

        #endregion
    }
}