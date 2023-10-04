using ABI_RC.Core.Networking;
using ABI_RC.Systems.ModNetwork;
using DarkRift;
using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Networking;

// overcomplicated, but functional
// a

public static class ModNetwork
{
    public static bool Debug_NetworkInbound = false;
    public static bool Debug_NetworkOutbound = false;

    private static bool _isSubscribedToModNetwork;
    
    #region Constants

    private const string ModId = "MelonMod.NAK.AvatarScaleMod";
    private const float SendRateLimit = 0.25f;
    private const float ReceiveRateLimit = 0.2f;
    private const int MaxWarnings = 2;
    private const float TimeoutDuration = 10f;
    
    private class QueuedMessage
    {
        public MessageType Type { get; set; }
        public float Height { get; set; }
        public string TargetPlayer { get; set; }
        public string Sender { get; set; }
    }

    #endregion

    #region Private State

    private static readonly Dictionary<string, QueuedMessage> OutboundQueue = new();
    private static float LastSentTime;

    private static readonly Dictionary<string, QueuedMessage> InboundQueue = new();
    private static readonly Dictionary<string, float> LastReceivedTimes = new();

    private static readonly Dictionary<string, int> UserWarnings = new();
    private static readonly Dictionary<string, float> UserTimeouts = new();

    #endregion

    #region Enums

    private enum MessageType : byte
    {
        SyncHeight = 0, // just send height
        RequestHeight = 1 // send height, request height back
    }

    #endregion

    #region Mod Network Internals

    internal static void Subscribe()
    {
        _isSubscribedToModNetwork = true;
        ModNetworkManager.Subscribe(ModId, OnMessageReceived);
    }

    internal static void Update()
    {
        if (!_isSubscribedToModNetwork)
            return;
        
        ProcessOutboundQueue();
        ProcessInboundQueue();
    }

    private static void SendMessage(MessageType messageType, float height, string playerId = null)
    {
        if (!IsConnectedToGameNetwork())
            return;

        if (!Enum.IsDefined(typeof(MessageType), messageType))
            return;
        
        if (!string.IsNullOrEmpty(playerId))
        {
            // to specific user
            using ModNetworkMessage modMsg = new(ModId, playerId);
            modMsg.Write((byte)messageType);
            modMsg.Write(height);
            modMsg.Send();
        }
        else
        {
            // to all users
            using ModNetworkMessage modMsg = new(ModId);
            modMsg.Write((byte)messageType);
            modMsg.Write(height);
            modMsg.Send();
        }
    }

    private static void OnMessageReceived(ModNetworkMessage msg)
    {
        msg.Read(out byte msgTypeRaw);
        msg.Read(out float receivedHeight);

        if (!Enum.IsDefined(typeof(MessageType), msgTypeRaw))
            return;

        // User is in timeout
        if (UserTimeouts.TryGetValue(msg.Sender, out var timeoutEnd) && Time.time < timeoutEnd)
            return;

        if (IsRateLimited(msg.Sender))
            return;

        QueuedMessage inboundMessage = new()
        {
            Type = (MessageType)msgTypeRaw,
            Height = receivedHeight,
            Sender = msg.Sender
        };

        InboundQueue[msg.Sender] = inboundMessage;
    }

    #endregion

    #region Public Methods

    public static void SendNetworkHeight(float newHeight)
    {
        OutboundQueue["global"] = new QueuedMessage { Type = MessageType.SyncHeight, Height = newHeight };
    }

    public static void RequestHeightSync()
    {
        var myCurrentHeight = AvatarScaleManager.Instance.GetHeightForNetwork();
        OutboundQueue["global"] = new QueuedMessage { Type = MessageType.RequestHeight, Height = myCurrentHeight };
    }

    #endregion

    #region Outbound Height Queue

    private static void ProcessOutboundQueue()
    {
        if (OutboundQueue.Count == 0 || Time.time - LastSentTime < SendRateLimit)
            return;

        foreach (QueuedMessage message in OutboundQueue.Values)
        {
            SendMessage(message.Type, message.Height, message.TargetPlayer);
            
            if (Debug_NetworkOutbound)
                AvatarScaleMod.Logger.Msg(
                    $"Sending message {message.Type.ToString()} to {(string.IsNullOrEmpty(message.TargetPlayer) ? "ALL" : message.TargetPlayer)}: {message.Height}");
        }

        OutboundQueue.Clear();
        LastSentTime = Time.time;
    }

    #endregion

    #region Inbound Height Queue

    private static bool IsRateLimited(string userId)
    {
        // Rate-limit checking
        if (LastReceivedTimes.TryGetValue(userId, out var lastReceivedTime) &&
            Time.time - lastReceivedTime < ReceiveRateLimit)
        {
            if (UserWarnings.TryGetValue(userId, out var warnings))
            {
                warnings++;
                UserWarnings[userId] = warnings;

                if (warnings >= MaxWarnings)
                {
                    UserTimeouts[userId] = Time.time + TimeoutDuration;
                    AvatarScaleMod.Logger.Warning($"User is sending height updates too fast! Applying 10s timeout... : {userId}");
                    return true;
                }
            }
            else
            {
                UserWarnings[userId] = 1;
            }

            return true;
        }

        LastReceivedTimes[userId] = Time.time;
        UserWarnings.Remove(userId); // Reset warnings
        return false;
    }

    private static void ProcessInboundQueue()
    {
        foreach (QueuedMessage message in InboundQueue.Values)
        {
            switch (message.Type)
            {
                case MessageType.RequestHeight:
                {
                    var myNetworkHeight = AvatarScaleManager.Instance.GetHeightForNetwork();
                    OutboundQueue[message.Sender] = new QueuedMessage
                    {
                        Type = MessageType.SyncHeight,
                        Height = myNetworkHeight,
                        TargetPlayer = message.Sender
                    };

                    AvatarScaleManager.Instance.OnNetworkHeightUpdateReceived(message.Sender, message.Height);
                    break;
                }
                case MessageType.SyncHeight:
                    AvatarScaleManager.Instance.OnNetworkHeightUpdateReceived(message.Sender, message.Height);
                    break;
                default:
                    AvatarScaleMod.Logger.Error($"Invalid message type received from: {message.Sender}");
                    break;
            }
            
            if (Debug_NetworkInbound)
                AvatarScaleMod.Logger.Msg($"Received message {message.Type.ToString()} from {message.Sender}: {message.Height}");
        }

        InboundQueue.Clear();
    }

    #endregion

    #region Private Methods

    private static bool IsConnectedToGameNetwork()
    {
        return NetworkManager.Instance != null
               && NetworkManager.Instance.GameNetwork != null
               && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }

    #endregion
}