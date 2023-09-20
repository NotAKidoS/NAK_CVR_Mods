using ABI_RC.Systems.ModNetwork;
using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Networking;

public static class ModNetwork
{
    #region Constants

    private const string ModId = "MelonMod.NAK.AvatarScaleMod";
    private const float SendRateLimit = 0.25f;
    private const float ReceiveRateLimit = 0.2f;
    private const int MaxWarnings = 2;
    private const float TimeoutDuration = 10f;

    #endregion

    #region Private State

    private static float? OutboundQueue;
    private static float LastSentTime;

    private static readonly Dictionary<string, float> InboundQueue = new Dictionary<string, float>();
    private static readonly Dictionary<string, float> LastReceivedTimes = new Dictionary<string, float>();

    private static readonly Dictionary<string, int> UserWarnings = new Dictionary<string, int>();
    private static readonly Dictionary<string, float> UserTimeouts = new Dictionary<string, float>();

    #endregion

    #region Mod Network Internals

    internal static void Subscribe()
    {
        ModNetworkManager.Subscribe(ModId, OnMessageReceived);
    }
    
    internal static void Update()
    {
        ProcessOutboundQueue();
        ProcessInboundQueue();
    }

    private static void SendMessageToAll(float height)
    {
        using ModNetworkMessage modMsg = new ModNetworkMessage(ModId);
        modMsg.Write(height);
        modMsg.Send();
        //MelonLogger.Msg($"Sending height: {height}");
    }

    private static void OnMessageReceived(ModNetworkMessage msg)
    {
        msg.Read(out float receivedHeight);
        ProcessReceivedHeight(msg.Sender, receivedHeight);
        //MelonLogger.Msg($"Received height from {msg.Sender}: {receivedHeight}");
    }

    #endregion

    #region Public Methods

    public static void SendNetworkHeight(float newHeight)
    {
        OutboundQueue = newHeight;
    }

    #endregion

    #region Outbound Height Queue

    private static void ProcessOutboundQueue()
    {
        if (!OutboundQueue.HasValue) 
            return;

        if (!(Time.time - LastSentTime >= SendRateLimit))
            return;

        SendMessageToAll(OutboundQueue.Value);
        LastSentTime = Time.time;
        OutboundQueue = null;
    }

    #endregion

    #region Inbound Height Queue

    private static void ProcessReceivedHeight(string userId, float receivedHeight)
    {
        // User is in timeout
        if (UserTimeouts.TryGetValue(userId, out float timeoutEnd) && Time.time < timeoutEnd)
            return;

        // Rate-limit checking
        if (LastReceivedTimes.TryGetValue(userId, out float lastReceivedTime) &&
            Time.time - lastReceivedTime < ReceiveRateLimit)
        {
            if (UserWarnings.TryGetValue(userId, out int warnings))
            {
                warnings++;
                UserWarnings[userId] = warnings;

                if (warnings >= MaxWarnings)
                {
                    UserTimeouts[userId] = Time.time + TimeoutDuration;
                    MelonLogger.Msg($"User is sending height updates too fast! Applying 10s timeout... : {userId}");
                    return;
                }
            }
            else
            {
                UserWarnings[userId] = 1;
            }
        }
        else
        {
            LastReceivedTimes[userId] = Time.time;
            UserWarnings.Remove(userId); // Reset warnings
            // MelonLogger.Msg($"Clearing timeout from user : {userId}");
        }

        InboundQueue[userId] = receivedHeight;
    }

    private static void ProcessInboundQueue()
    {
        foreach (var (userId, height) in InboundQueue)
        {
            MelonLogger.Msg($"Applying inbound queued height {height} from : {userId}");
            AvatarScaleManager.Instance.OnNetworkHeightUpdateReceived(userId, height);
        }

        InboundQueue.Clear();
    }

    #endregion
}