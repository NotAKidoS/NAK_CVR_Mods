using System.Net;

namespace NAK.ShareBubbles.API.Exceptions;

public class ShareApiException : Exception
{
    public HttpStatusCode StatusCode { get; } // TODO: network back status code to claiming client, to show why the request failed
    public string UserFriendlyMessage { get; }

    public ShareApiException(HttpStatusCode statusCode, string message, string userFriendlyMessage) 
        : base(message)
    {
        StatusCode = statusCode;
        UserFriendlyMessage = userFriendlyMessage;
    }
}

public class ContentNotSharedException : ShareApiException
{
    public ContentNotSharedException(string contentId) 
        : base(HttpStatusCode.BadRequest, 
            $"Content {contentId} is not currently shared", 
            "This content is not currently shared with anyone")
    {
    }
}

public class ContentNotFoundException : ShareApiException
{
    public ContentNotFoundException(string contentId) 
        : base(HttpStatusCode.NotFound, 
            $"Content {contentId} not found", 
            "The specified content could not be found")
    {
    }
}

public class UserOnlyAllowsSharesFromFriendsException : ShareApiException
{
    public UserOnlyAllowsSharesFromFriendsException(string userId) 
        : base(HttpStatusCode.Forbidden, 
            $"User {userId} only accepts shares from friends", 
            "This user only accepts shares from friends")
    {
    }
}

public class UserNotFoundException : ShareApiException
{
    public UserNotFoundException(string userId) 
        : base(HttpStatusCode.NotFound, 
            $"User {userId} not found", 
            "The specified user could not be found")
    {
    }
}

public class ContentAlreadySharedException : ShareApiException
{
    public ContentAlreadySharedException(string contentId, string userId) 
        : base(HttpStatusCode.Conflict, 
            $"Content {contentId} is already shared with user {userId}", 
            "This content is already shared with this user")
    {
    }
}