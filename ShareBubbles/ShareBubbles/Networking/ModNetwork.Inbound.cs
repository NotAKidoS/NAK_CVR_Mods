using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.ModNetwork;
using NAK.ShareBubbles.API;
using UnityEngine;

namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Reset Method
    
    public static void Reset()
    {
        LoggerInbound("ModNetwork has been reset.");
    }

    #endregion Reset Method

    #region Inbound Methods
    
    private static bool ShouldReceiveFromSender(string sender)
    {
        // if (_disallowedForSession.Contains(sender))
        //     return false; // ignore messages from disallowed users

        if (MetaPort.Instance.blockedUserIds.Contains(sender))
            return false; // ignore messages from blocked users
        
        // if (ModSettings.Entry_FriendsOnly.Value && !Friends.FriendsWith(sender))
        //     return false; // ignore messages from non-friends if friends only is enabled
        
        // if (StickerSystem.Instance.IsRestrictedInstance) // ignore messages from users when the world is restricted. This also includes older or modified version of Stickers mod.
        //     return false;
        
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
              case MessageType.BubbleCreated:
                    HandleBubbleCreated(msg);
                    break;
                case MessageType.BubbleDestroyed:
                    HandleBubbleDestroyed(msg);
                    break;
                case MessageType.BubbleMoved:
                    HandleBubbleMoved(msg);
                    break;
                case MessageType.BubbleClaimRequest:
                    HandleBubbleClaimRequest(msg);
                    break;
                case MessageType.BubbleClaimResponse:
                    HandleBubbleClaimResponse(msg);
                    break;
                case MessageType.ActiveBubblesRequest:
                    HandleActiveBubblesRequest(msg);
                    break;
                case MessageType.ActiveBubblesResponse:
                    HandleActiveBubblesResponse(msg);
                    break;
                case MessageType.DirectShareNotification:
                    HandleDirectShareNotification(msg);
                    break;
                default:
                    LoggerInbound($"Invalid message type received: {msgTypeRaw}");
                    break;
            }
        }
        catch (Exception e)
        {
            LoggerInbound($"Error handling message from {msg.Sender}: {e.Message}", MNLogLevel.Warning);
        }
    }

    private static void HandleBubbleCreated(ModNetworkMessage msg)
    {
        msg.Read(out Vector3 position);
        msg.Read(out Vector3 rotation);
        msg.Read(out ShareBubbleData data);
        
        // Check if the position or rotation is invalid
        if (IsInvalidVector3(position) 
            || IsInvalidVector3(rotation))
            return;
        
        // Check if we should ignore this bubble
        // Client enforced, sure, but only really matters for share claim, which is requires bubble owner to verify
        if (data.Rule == ShareRule.FriendsOnly && !Friends.FriendsWith(msg.Sender))
        {
            LoggerInbound($"Bubble with ID {data.BubbleId} is FriendsOnly and sender is not a friend, ignoring.");
            return;
        }
        
        ShareBubbleManager.Instance.OnRemoteBubbleCreated(msg.Sender, position, rotation, data);
    
        LoggerInbound($"Bubble with ID {data.BubbleId} created at {position}");
    }
    
    private static void HandleBubbleDestroyed(ModNetworkMessage msg)
    {
        msg.Read(out uint bubbleNetworkId);
        
        // Destroy bubble
        ShareBubbleManager.Instance.OnRemoteBubbleDestroyed(msg.Sender, bubbleNetworkId);
        
        LoggerInbound($"Bubble with ID {bubbleNetworkId} destroyed");
    }
    
    
    private static void HandleBubbleMoved(ModNetworkMessage msg)
    {
        msg.Read(out uint bubbleId);
        msg.Read(out Vector3 position);
        msg.Read(out Vector3 rotation);
        
        // Check if the position or rotation is invalid
        if (IsInvalidVector3(position) 
            || IsInvalidVector3(rotation))
            return;

        ShareBubbleManager.Instance.OnRemoteBubbleMoved(msg.Sender, bubbleId, position, rotation);
        LoggerInbound($"Bubble {bubbleId} moved to {position}");
    }
    
    private static void HandleBubbleClaimRequest(ModNetworkMessage msg)
    {
        msg.Read(out uint bubbleNetworkId);
        
        ShareBubbleManager.Instance.OnRemoteBubbleClaimRequest(msg.Sender, bubbleNetworkId);
        
        LoggerInbound($"Bubble with ID {bubbleNetworkId} claimed by {msg.Sender}");
    }
    
    private static void HandleBubbleClaimResponse(ModNetworkMessage msg)
    {
        msg.Read(out uint bubbleNetworkId);
        msg.Read(out bool claimAccepted);
        
        ShareBubbleManager.Instance.OnRemoteBubbleClaimResponse(msg.Sender, bubbleNetworkId, claimAccepted);
        
        LoggerInbound($"Bubble with ID {bubbleNetworkId} claim response: {claimAccepted}");
    }
    
    private static void HandleActiveBubblesRequest(ModNetworkMessage msg)
    {
        LoggerInbound($"Received ActiveBubblesRequest from {msg.Sender}");
        
        ShareBubbleManager.Instance.OnRemoteActiveBubbleRequest(msg.Sender);
    }

    private static void HandleActiveBubblesResponse(ModNetworkMessage msg)
    {
        try
        {
            // hacky, but im tired and didnt think
            ShareBubbleManager.Instance.SetRemoteBatchCreateBubbleState(msg.Sender, true);
            
            msg.Read(out int bubbleCount);
            LoggerInbound($"Received ActiveBubblesResponse from {msg.Sender} with {bubbleCount} bubbles");
            
            // Create up to MaxBubblesPerUser bubbles
            for (int i = 0; i < Mathf.Min(bubbleCount, ShareBubbleManager.MaxBubblesPerUser); i++)
                HandleBubbleCreated(msg);
        }
        catch (Exception e)
        {
            LoggerInbound($"Error handling ActiveBubblesResponse from {msg.Sender}: {e.Message}", MNLogLevel.Warning);
        }
        finally
        {
            ShareBubbleManager.Instance.SetRemoteBatchCreateBubbleState(msg.Sender, false);
        }
    }
    
    private static void HandleDirectShareNotification(ModNetworkMessage msg)
    {
        msg.Read(out ShareApiHelper.ShareContentType contentType);
        msg.Read(out string contentId);
        
        LoggerInbound($"Received DirectShareNotification from {msg.Sender} for {contentType} {contentId}");
    }
    
    #endregion Inbound Methods
}