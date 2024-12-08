using ABI_RC.Systems.ModNetwork;
using NAK.ShareBubbles.API;
using UnityEngine;

namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Outbound Methods

    public static void SendBubbleCreated(Vector3 position, Quaternion rotation, ShareBubbleData data)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.BubbleCreated);
        modMsg.Write(position);
        modMsg.Write(rotation.eulerAngles);
        modMsg.Write(data);
        modMsg.Send();

        LoggerOutbound($"Sending BubbleCreated message for bubble {data.BubbleId}");
    }
    
    public static void SendBubbleDestroyed(uint bubbleNetworkId)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.BubbleDestroyed);
        modMsg.Write(bubbleNetworkId);
        modMsg.Send();

        LoggerOutbound($"Sending BubbleDestroyed message for bubble {bubbleNetworkId}");
    }
    
    public static void SendBubbleMove(int bubbleId, Vector3 position, Quaternion rotation)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage msg = new(ModId);
        msg.Write((byte)MessageType.BubbleMoved);
        msg.Write(bubbleId);
        msg.Write(position);
        msg.Write(rotation.eulerAngles);
        msg.Send();

        LoggerOutbound($"Sending BubbleMove message for bubble {bubbleId}");
    }
    
    public static async Task<ClaimResponseType> SendBubbleClaimRequestAsync(string bubbleOwnerId, uint bubbleNetworkId)
    {
        if (!CanSendModNetworkMessage())
            return ClaimResponseType.Rejected;

        // Create pending request
        PendingClaimRequest request = new(bubbleNetworkId);
        _pendingClaimRequests[bubbleNetworkId] = request;

        // Send request
        using (ModNetworkMessage modMsg = new(ModId, bubbleOwnerId))
        {
            modMsg.Write((byte)MessageType.BubbleClaimRequest);
            modMsg.Write(bubbleNetworkId);
            modMsg.Send();
        }

        LoggerOutbound($"Sending BubbleClaimRequest message for bubble {bubbleNetworkId}");

        try
        {
            // Wait for response with timeout
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(ClaimRequestTimeout));
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(ClaimRequestTimeout), cts.Token);
            var responseTask = request.CompletionSource.Task;
            
            Task completedTask = await Task.WhenAny(responseTask, timeoutTask);
            if (completedTask == timeoutTask) return ClaimResponseType.Timeout;
            
            return await responseTask;
        }
        finally
        {
            _pendingClaimRequests.Remove(bubbleNetworkId);
        }
    }
    
    public static void SendBubbleClaimResponse(string requesterUserId, uint bubbleNetworkId, ClaimResponseType responseType)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId, requesterUserId);
        modMsg.Write((byte)MessageType.BubbleClaimResponse);
        modMsg.Write(bubbleNetworkId);
        modMsg.Write((byte)responseType);
        modMsg.Send();

        LoggerOutbound($"Sending BubbleClaimResponse message for bubble {bubbleNetworkId}: {responseType}");
    }
    
    public static void SendActiveBubblesRequest()
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId);
        modMsg.Write((byte)MessageType.ActiveBubblesRequest);
        modMsg.Send();

        LoggerOutbound("Sending ActiveBubblesRequest message");
    }
    
    public static void SendActiveBubblesResponse(string requesterUserId, List<ShareBubble> activeBubbles)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId, requesterUserId);
        modMsg.Write((byte)MessageType.ActiveBubblesResponse);
        modMsg.Write(activeBubbles.Count);

        foreach (ShareBubble bubble in activeBubbles)
        {
            Transform parent = bubble.transform.parent;
            modMsg.Write(parent.position);
            modMsg.Write(parent.rotation.eulerAngles);
            modMsg.Write(bubble.Data);
        }
    
        modMsg.Send();

        LoggerOutbound($"Sending ActiveBubblesResponse message with {activeBubbles.Count} bubbles");
    }

    public static void SendDirectShareNotification(string userId, ShareApiHelper.ShareContentType contentType, string contentId)
    {
        if (!CanSendModNetworkMessage())
            return;

        using ModNetworkMessage modMsg = new(ModId, userId);
        modMsg.Write((byte)MessageType.DirectShareNotification);
        modMsg.Write(contentType);
        modMsg.Write(contentId);
        modMsg.Send();

        LoggerOutbound($"Sending DirectShareNotification message for {userId}");
    }
 
    #endregion Outbound Methods
}