using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Savior;
using NAK.ShareBubbles.API.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NAK.ShareBubbles.API;

/// <summary>
/// API for content sharing management.
/// </summary>
public static class ShareApiHelper
{
    #region Enums

    public enum ShareContentType
    {
        Avatar,
        Spawnable
    }

    private enum ShareApiOperation
    {
        ShareAvatar,
        ReleaseAvatar,
        
        ShareSpawnable,
        ReleaseSpawnable,
        
        GetAvatarShares,
        GetSpawnableShares
    }

    #endregion Enums

    #region Public API

    /// <summary>
    /// Shares content with a specified user.
    /// </summary>
    /// <param name="type">Type of content to share</param>
    /// <param name="contentId">ID of the content</param>
    /// <param name="userId">Target user ID</param>
    /// <returns>Response containing share information</returns>
    /// <exception cref="ShareApiException">Thrown when API request fails</exception>
    /// <exception cref="UserNotFoundException">Thrown when target user is not found</exception>
    /// <exception cref="ContentNotFoundException">Thrown when content is not found</exception>
    /// <exception cref="UserOnlyAllowsSharesFromFriendsException">Thrown when user only accepts shares from friends</exception>
    /// <exception cref="ContentAlreadySharedException">Thrown when content is already shared with user</exception>
    public static Task<BaseResponse<T>> ShareContentAsync<T>(ShareContentType type, string contentId, string userId)
    {
        ShareApiOperation operation = type == ShareContentType.Avatar
            ? ShareApiOperation.ShareAvatar
            : ShareApiOperation.ShareSpawnable;

        ShareRequest data = new()
        {
            ContentId = contentId,
            UserId = userId
        };

        return MakeApiRequestAsync<T>(operation, data);
    }

    /// <summary>
    /// Releases shared content from a specified user.
    /// </summary>
    /// <param name="type">Type of content to release</param>
    /// <param name="contentId">ID of the content</param>
    /// <param name="userId">Optional user ID. If null, releases share from self</param>
    /// <returns>Response indicating success</returns>
    /// <exception cref="ShareApiException">Thrown when API request fails</exception>
    /// <exception cref="ContentNotSharedException">Thrown when content is not shared</exception>
    /// <exception cref="ContentNotFoundException">Thrown when content is not found</exception>
    /// <exception cref="UserNotFoundException">Thrown when specified user is not found</exception>
    public static Task<BaseResponse<T>> ReleaseShareAsync<T>(ShareContentType type, string contentId, string userId = null)
    {
        ShareApiOperation operation = type == ShareContentType.Avatar
            ? ShareApiOperation.ReleaseAvatar
            : ShareApiOperation.ReleaseSpawnable;
        
        // If no user ID is provided, release share from self
        userId ??= MetaPort.Instance.ownerId;

        ShareRequest data = new()
        {
            ContentId = contentId,
            UserId = userId
        };

        return MakeApiRequestAsync<T>(operation, data);
    }

    /// <summary>
    /// Gets all shares for specified content.
    /// </summary>
    /// <param name="type">Type of content</param>
    /// <param name="contentId">ID of the content</param>
    /// <returns>Response containing share information</returns>
    /// <exception cref="ShareApiException">Thrown when API request fails</exception>
    /// <exception cref="ContentNotFoundException">Thrown when content is not found</exception>
    public static Task<BaseResponse<T>> GetSharesAsync<T>(ShareContentType type, string contentId)
    {
        ShareApiOperation operation = type == ShareContentType.Avatar
            ? ShareApiOperation.GetAvatarShares
            : ShareApiOperation.GetSpawnableShares;

        ShareRequest data = new() { ContentId = contentId };
        return MakeApiRequestAsync<T>(operation, data);
    }

    #endregion Public API

    #region Private Implementation

    [Serializable]
    private record ShareRequest
    {
        public string ContentId { get; set; }
        public string UserId { get; set; }
    }

    private static async Task<BaseResponse<T>> MakeApiRequestAsync<T>(ShareApiOperation operation, ShareRequest data)
    {
        ValidateAuthenticationState();

        (string endpoint, HttpMethod method) = GetApiEndpointAndMethod(operation, data);
        using HttpRequestMessage request = CreateHttpRequest(endpoint, method, data);

        try
        {
            using HttpResponseMessage response = await ApiConnection._client.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();
            
            return HandleApiResponse<T>(response, content, data.ContentId, data.UserId);
        }
        catch (HttpRequestException ex)
        {
            throw new ShareApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Failed to communicate with the server: {ex.Message}",
                "Unable to connect to the server. Please check your internet connection.");
        }
        catch (JsonException ex)
        {
            throw new ShareApiException(
                HttpStatusCode.UnprocessableEntity,
                $"Failed to process response data: {ex.Message}",
                "Server returned invalid data. Please try again later.");
        }
    }

    private static void ValidateAuthenticationState()
    {
        if (!AuthManager.IsAuthenticated)
        {
            throw new ShareApiException(
                HttpStatusCode.Unauthorized,
                "User is not authenticated",
                "Please log in to perform this action");
        }
    }

    private static HttpRequestMessage CreateHttpRequest(string endpoint, HttpMethod method, ShareRequest data)
    {
        HttpRequestMessage request = new(method, endpoint);

        if (method == HttpMethod.Post)
        {
            JObject json = JObject.FromObject(data);
            request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static BaseResponse<T> HandleApiResponse<T>(HttpResponseMessage response, string content, string contentId, string userId)
    {
        if (response.IsSuccessStatusCode)
            return CreateSuccessResponse<T>(content);

        // Let specific exceptions propagate up to the caller
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ContentNotSharedException(contentId),
            HttpStatusCode.NotFound when userId != null => new UserNotFoundException(userId),
            HttpStatusCode.NotFound => new ContentNotFoundException(contentId),
            HttpStatusCode.Forbidden => new UserOnlyAllowsSharesFromFriendsException(userId),
            HttpStatusCode.Conflict => new ContentAlreadySharedException(contentId, userId),
            _ => new ShareApiException(
                response.StatusCode,
                $"API request failed with status {response.StatusCode}: {content}",
                "An unexpected error occurred. Please try again later.")
        };
    }

    private static BaseResponse<T> CreateSuccessResponse<T>(string content)
    {
        var response = new BaseResponse<T>("")
        {
            IsSuccessStatusCode = true,
            HttpStatusCode = HttpStatusCode.OK
        };

        if (!string.IsNullOrEmpty(content))
        {
            response.Data = JsonConvert.DeserializeObject<T>(content);
        }

        return response;
    }

    private static (string endpoint, HttpMethod method) GetApiEndpointAndMethod(ShareApiOperation operation, ShareRequest data)
    {
        string baseUrl = $"{ApiConnection.APIAddress}/{ApiConnection.APIVersion}";
        string encodedContentId = HttpUtility.UrlEncode(data.ContentId);
        
        return operation switch
        {
            ShareApiOperation.GetAvatarShares => ($"{baseUrl}/avatars/{encodedContentId}/shares", HttpMethod.Get),
            ShareApiOperation.ShareAvatar => ($"{baseUrl}/avatars/{encodedContentId}/shares/{HttpUtility.UrlEncode(data.UserId)}", HttpMethod.Post),
            ShareApiOperation.ReleaseAvatar => ($"{baseUrl}/avatars/{encodedContentId}/shares/{HttpUtility.UrlEncode(data.UserId)}", HttpMethod.Delete),
            ShareApiOperation.GetSpawnableShares => ($"{baseUrl}/spawnables/{encodedContentId}/shares", HttpMethod.Get),
            ShareApiOperation.ShareSpawnable => ($"{baseUrl}/spawnables/{encodedContentId}/shares/{HttpUtility.UrlEncode(data.UserId)}", HttpMethod.Post),
            ShareApiOperation.ReleaseSpawnable => ($"{baseUrl}/spawnables/{encodedContentId}/shares/{HttpUtility.UrlEncode(data.UserId)}", HttpMethod.Delete),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
    }

    #endregion Private Implementation
}