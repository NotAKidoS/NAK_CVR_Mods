using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;

namespace NAK.PropsButBetter;

public enum PedestalType
{
    Avatar,
    Prop
}

public static class PedestalInfoBatchProcessor
{
    private class CachedResponse
    {
        public PedestalInfoResponse Response;
        public DateTime CachedAt;
    }

    private static readonly Dictionary<PedestalType, Dictionary<string, CachedResponse>> _cache = new()
    {
        { PedestalType.Avatar, new Dictionary<string, CachedResponse>() },
        { PedestalType.Prop, new Dictionary<string, CachedResponse>() }
    };

    private static readonly Dictionary<PedestalType, Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>> _pendingRequests = new()
    {
        { PedestalType.Avatar, new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>() },
        { PedestalType.Prop, new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>() }
    };

    private static readonly Dictionary<PedestalType, bool> _isBatchProcessing = new()
    {
        { PedestalType.Avatar, false },
        { PedestalType.Prop, false }
    };

    private static readonly object _lock = new();
    private const float BATCH_DELAY = 2f;
    private const float CACHE_DURATION = 300f; // 5 minutes

    public static Task<PedestalInfoResponse> QueuePedestalInfoRequest(PedestalType type, string contentId, bool skipDelayIfNotCached = false)
    {
        lock (_lock)
        {
            // Check cache first
            if (_cache[type].TryGetValue(contentId, out var cached))
            {
                if ((DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_DURATION)
                    return Task.FromResult(cached.Response);
                
                _cache[type].Remove(contentId);
            }

            // Check if already pending
            var requests = _pendingRequests[type];
            if (requests.TryGetValue(contentId, out var existingTcs))
                return existingTcs.Task;

            // Queue new request
            var tcs = new TaskCompletionSource<PedestalInfoResponse>();
            requests[contentId] = tcs;

            if (!_isBatchProcessing[type])
            {
                _isBatchProcessing[type] = true;
                ProcessBatchAfterDelay(type, skipDelayIfNotCached);
            }

            return tcs.Task;
        }
    }

    private static async void ProcessBatchAfterDelay(PedestalType type, bool skipDelayIfNotCached)
    {
        if (!skipDelayIfNotCached)
            await Task.Delay(TimeSpan.FromSeconds(BATCH_DELAY));

        List<string> contentIds;
        Dictionary<string, TaskCompletionSource<PedestalInfoResponse>> requestBatch;

        lock (_lock)
        {
            contentIds = _pendingRequests[type].Keys.ToList();
            requestBatch = new Dictionary<string, TaskCompletionSource<PedestalInfoResponse>>(_pendingRequests[type]);
            _pendingRequests[type].Clear();
            _isBatchProcessing[type] = false;
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
                var now = DateTime.UtcNow;

                lock (_lock)
                {
                    foreach (var kvp in requestBatch)
                    {
                        if (responseDict.TryGetValue(kvp.Key, out var info))
                        {
                            _cache[type][kvp.Key] = new CachedResponse { Response = info, CachedAt = now };
                            kvp.Value.SetResult(info);
                        }
                        else
                        {
                            kvp.Value.SetException(new Exception($"Content info not found for ID: {kvp.Key}"));
                        }
                    }
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