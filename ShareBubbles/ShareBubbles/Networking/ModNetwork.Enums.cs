namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Enums

    // Remotes will ask owner of a bubble to share its content
    // Owner can accept or deny the request, and the result will be sent back to the remote
    // TODO: need rate limiting to prevent malicious users from spamming requests

    private enum MessageType : byte
    {
        // Lifecycle of a bubble
        BubbleCreated,          // bubbleId, bubbleType, position, rotation
        BubbleDestroyed,        // bubbleId
        BubbleMoved,            // bubbleId, position, rotation
        
        // Requesting share of a bubbles content
        BubbleClaimRequest,     // bubbleId
        BubbleClaimResponse,    // bubbleId, success
        
        // Requesting all active bubbles on instance join
        ActiveBubblesRequest,      // none
        ActiveBubblesResponse,     // int count, bubbleId[count], bubbleType[count], position[count], rotation[count]
        
        // Notification of share being sent to a user
        DirectShareNotification,   // userId, contentType, contentId
    }
    
    private enum MNLogLevel : byte
    {
        Info,
        Warning,
        Error
    }
    
    public enum ClaimResponseType : byte
    {
        Accepted,
        Rejected,
        NotAcceptingSharesFromNonFriends,
        AlreadyShared,
        Timeout
    }

    #endregion Enums
}