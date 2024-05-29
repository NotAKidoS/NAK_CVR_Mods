using ABI_RC.Core.Networking;
using ABI_RC.Systems.ModNetwork;
using DarkRift;
using UnityEngine;

namespace NAK.RelativeSync.Networking;

public static class ModNetwork
{
    public static bool Debug_NetworkInbound = false;
    public static bool Debug_NetworkOutbound = false;

    private static bool _isSubscribedToModNetwork;

    private struct MovementParentSyncData
    {
        public bool HasSyncedThisData;
        public int MarkerHash;
        public Vector3 RootPosition;
        public Vector3 RootRotation;
        // public Vector3 HipPosition;
        // public Vector3 HipRotation;
    }

    private static MovementParentSyncData _latestMovementParentSyncData;

    #region Constants

    private const string ModId = "MelonMod.NAK.RelativeSync";

    #endregion

    #region Enums

    private enum MessageType : byte
    {
        MovementParentOrChair = 0
        //RelativePickup = 1,
        //RelativeAttachment = 2,
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

    // Called right after NetworkRootDataUpdate.Submit()
    internal static void SendRelativeSyncUpdate()
    {
        if (!_isSubscribedToModNetwork)
            return;

        if (_latestMovementParentSyncData.HasSyncedThisData)
            return;

        SendMessage(MessageType.MovementParentOrChair, _latestMovementParentSyncData.MarkerHash,
            _latestMovementParentSyncData.RootPosition, _latestMovementParentSyncData.RootRotation);
        
        _latestMovementParentSyncData.HasSyncedThisData = true;
    }

    public static void SetLatestRelativeSync(
        int markerHash, 
        Vector3 position, Vector3 rotation)
    {
        // check if the data has changed
        if (_latestMovementParentSyncData.MarkerHash == markerHash
            && _latestMovementParentSyncData.RootPosition == position
            && _latestMovementParentSyncData.RootRotation == rotation)
            return; // no need to update (shocking)

        _latestMovementParentSyncData.HasSyncedThisData = false; // reset
        _latestMovementParentSyncData.MarkerHash = markerHash;
        _latestMovementParentSyncData.RootPosition = position;
        _latestMovementParentSyncData.RootRotation = rotation;
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

        if (Debug_NetworkOutbound)
            Debug.Log(
                $"[Outbound] MessageType: {messageType}, MarkerHash: {markerHash}, Position: {position}, " +
                $"Rotation: {rotation}");
    }

    private static void OnMessageReceived(ModNetworkMessage msg)
    {
        msg.Read(out byte msgTypeRaw);

        if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw))
            return;

        switch ((MessageType)msgTypeRaw)
        {
            case MessageType.MovementParentOrChair:
                msg.Read(out int markerHash);
                msg.Read(out Vector3 receivedPosition);
                msg.Read(out Vector3 receivedRotation);
                // msg.Read(out Vector3 receivedHipPosition);
                // msg.Read(out Vector3 receivedHipRotation);
                
                OnNetworkPositionUpdateReceived(msg.Sender, markerHash, receivedPosition, receivedRotation);
                
                if (Debug_NetworkInbound)
                    Debug.Log($"[Inbound] Sender: {msg.Sender}, MarkerHash: {markerHash}, " +
                              $"Position: {receivedPosition}, Rotation: {receivedRotation}");
                break;
            default:
                Debug.LogError($"Invalid message type received from: {msg.Sender}");
                break;
        }
    }

    #endregion

    #region Private Methods

    private static bool IsConnectedToGameNetwork()
    {
        return NetworkManager.Instance != null
               && NetworkManager.Instance.GameNetwork != null
               && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }

    private static void OnNetworkPositionUpdateReceived(
        string sender, int markerHash, 
        Vector3 position, Vector3 rotation)
    {
        RelativeSyncManager.ApplyRelativeSync(sender, markerHash, position, rotation);
    }

    #endregion
}