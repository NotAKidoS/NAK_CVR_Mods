using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;

namespace NAK.ShareBubbles.API;

public enum PedestalType
{
    Avatar,
    Prop
}

/// <summary>
/// Batch processor for fetching pedestal info in bulk. The pedestal endpoints are meant to be used in batch, so might
/// as well handle bubble requests in batches if in the very unlikely case that multiple requests are made at once.
///
/// May only really matter for when a late joiner joins a room with a lot of bubbles.
///
/// Luc: if there is a lot being dropped, you could try to batch them
/// </summary>
public static class PedestalInfoBatchProcessor
{
    private static readonly Dictionary<PedestalType, Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>> _pendingRequests 
        = new()
        {
            { PedestalType.Avatar, new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>() },
            { PedestalType.Prop, new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>() }
        };
    
    private static readonly Dictionary<PedestalType, bool> _isBatchProcessing 
        = new()
        {
            { PedestalType.Avatar, false },
            { PedestalType.Prop, false }
        };
    
    // This breaks compile accepting this change.
    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private static readonly object _lock = new();
    private const float BATCH_DELAY = 2f;

    public static Task<PedestalInfoResponse> QueuePedestalInfoRequest(PedestalType type, string contentId)
    {
        var tcs = new TaskCompletionSource<PedestalInfoResponse>();
        
        lock (_lock)
        {
            var requests = _pendingRequests[type];
            
            if (!requests.TryAdd(contentId, tcs))
                return requests[contentId].Task;

            if (_isBatchProcessing[type]) 
                return tcs.Task;
            
            _isBatchProcessing[type] = true;
            ProcessBatchAfterDelay(type);
        }
        
        return tcs.Task;
    }

    private static async void ProcessBatchAfterDelay(PedestalType type)
    {
        await Task.Delay(TimeSpan.FromSeconds(BATCH_DELAY));
        
        List<string> contentIds;
        Dictionary<string, TaskCompletionSource<PedestalInfoResponse>> requestBatch;
        
        lock (_lock)
        {
            contentIds = _pendingRequests[type].Keys.ToList();
            requestBatch = new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>(_pendingRequests[type]);
            _pendingRequests[type].Clear();
            _isBatchProcessing[type] = false;
            //ShareBubblesMod.Logger.Msg($"Processing {type} pedestal info batch with {contentIds.Count} items");
        }

        try
        {
            ApiConnection.ApiOperation operation = type switch
            {
                PedestalType.Avatar => ApiConnection.ApiOperation.AvatarPedestal,
                PedestalType.Prop => ApiConnection.ApiOperation.PropPedestal,
                _ => throw new ArgumentException($"Unsupported pedestal type: {type}")
            };

            var response = await ApiConnection.MakeRequest<IEnumerable<PedestalInfoResponse>>(operation, contentIds);

            if (response?.Data != null)
            {
                var responseDict = response.Data.ToDictionary(info => info.Id);

                foreach (var kvp in requestBatch)
                {
                    if (responseDict.TryGetValue(kvp.Key, out var info))
                        kvp.Value.SetResult(info);
                    else
                        kvp.Value.SetException(new Exception($"Content info not found for ID: {kvp.Key}"));
                }
            }
            else
            {
                Exception exception = new($"Failed to fetch {type} info batch");
                foreach (var tcs in requestBatch.Values)
                    tcs.SetException(exception);
            }
        }
        catch (Exception ex)
        {
            foreach (var tcs in requestBatch.Values)
                tcs.SetException(ex);
        }
    }
}