using NAK.ShareBubbles.Networking;

namespace ShareBubbles.ShareBubbles.Implementation;

public class ShareClaimResult 
{
    public ModNetwork.ClaimResponseType ResponseType { get; }
    public bool RequiresSessionTracking { get; }

    private ShareClaimResult(ModNetwork.ClaimResponseType responseType, bool requiresSessionTracking = false)
    {
        ResponseType = responseType;
        RequiresSessionTracking = requiresSessionTracking;
    }

    public static ShareClaimResult Success(bool isSessionShare = false) 
        => new(ModNetwork.ClaimResponseType.Accepted, isSessionShare);
    
    public static ShareClaimResult AlreadyShared() 
        => new(ModNetwork.ClaimResponseType.AlreadyShared);
    
    public static ShareClaimResult FriendsOnly() 
        => new(ModNetwork.ClaimResponseType.NotAcceptingSharesFromNonFriends);
    
    public static ShareClaimResult Rejected() 
        => new(ModNetwork.ClaimResponseType.Rejected);
}