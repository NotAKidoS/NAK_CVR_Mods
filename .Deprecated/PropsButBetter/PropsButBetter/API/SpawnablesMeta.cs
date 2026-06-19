using System.Net;
using ABI_RC.Core;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Savior;
using NAK.PropsButBetter;
using Newtonsoft.Json;

public static class PropApiHelper
{
    /// <summary>
    /// Fetches the metadata for a specific prop/spawnable
    /// </summary>
    /// <param name="propId">The ID of the prop to fetch metadata for</param>
    /// <returns>BaseResponse containing UgcWithFile data</returns>
    public static async Task<BaseResponse<UgcWithFile>> GetPropMeta(string propId)
    {
        // Check authentication
        if (!AuthManager.IsAuthenticated)
        {
            PropsButBetterMod.Logger.Error("Attempted to fetch prop meta while user was not authenticated.");
            return new BaseResponse<UgcWithFile>("The user is not Authenticated.")
            {
                IsSuccessStatusCode = false,
                HttpStatusCode = HttpStatusCode.Unauthorized,
            };
        }

        // Construct the URL
        string requestUrl = $"{ApiConnection.APIAddress}/{ApiConnection.APIVersion}/spawnables/{propId}/meta";

        // Validate URL for security
        if (!CVRTools.IsSafeAbsoluteHttpUrl(requestUrl, out var uri))
        {
            PropsButBetterMod.Logger.Error($"Invalid URI was constructed! URI: {requestUrl}");
            return null;
        }

        // Create the HTTP request
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        
        // Add mature content header
        string allowMatureContent = MetaPort.Instance.matureContentAllowed ? "true" : "false";
        request.Headers.Add(ApiConnection.HeaderMatureContent, allowMatureContent);

        try
        {
            // Send the request
            HttpResponseMessage response = await ApiConnection.Client.SendAsync(request);

            // Handle successful response
            if (response.IsSuccessStatusCode)
            {
                var contentStr = await response.Content.ReadAsStringAsync();
                
                var baseResponse = string.IsNullOrWhiteSpace(contentStr)
                    ? new BaseResponse<UgcWithFile> { Message = "CVR_SUCCESS_PropMeta" }
                    : JsonConvert.DeserializeObject<BaseResponse<UgcWithFile>>(contentStr);
                    
                baseResponse.HttpStatusCode = response.StatusCode;
                baseResponse.IsSuccessStatusCode = true;

                return baseResponse;
            }

            // Handle failed response
            PropsButBetterMod.Logger.Warning($"Request failed with status {response.StatusCode} [{(int)response.StatusCode}]");

            string rawResponseBody = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<BaseResponse<UgcWithFile>>(rawResponseBody);
            
            if (res != null)
            {
                res.HttpStatusCode = response.StatusCode;
                res.IsSuccessStatusCode = false;
                return res;
            }
        }
        catch (Exception e)
        {
            PropsButBetterMod.Logger.Error($"Failed to fetch prop meta: {e.Message}");
        }

        return null;
    }
}