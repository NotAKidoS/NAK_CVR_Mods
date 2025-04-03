namespace NAK.BetterContentLoading.Queue;

public abstract class ContentDownloadQueueBase
{
    protected readonly struct QueueItem
    {
        public readonly DownloadInfo Info;
        public readonly float Priority;
        public readonly Action<string, string> OnComplete; // Callback with (downloadId, ownerId)

        public QueueItem(DownloadInfo info, float priority, Action<string, string> onComplete)
        {
            Info = info;
            Priority = priority;
            OnComplete = onComplete;
        }
    }

    protected readonly List<QueueItem> Queue = new();
    private readonly HashSet<string> ActiveDownloads = new(); // By DownloadId
    protected readonly Dictionary<string, HashSet<string>> DownloadOwners = new(); // DownloadId -> Set of OwnerIds
    private readonly SemaphoreSlim DownloadSemaphore;
    protected readonly BetterDownloadManager Manager;

    protected ContentDownloadQueueBase(BetterDownloadManager manager, int maxParallelDownloads)
    {
        Manager = manager;
        DownloadSemaphore = new SemaphoreSlim(maxParallelDownloads);
    }

    protected void QueueDownload(in DownloadInfo info, string ownerId, float priority, Action<string, string> onComplete)
    {
        if (Manager.IsDebugEnabled)
            BetterContentLoadingMod.Logger.Msg($"Attempting to queue download for {info.AssetId} (DownloadId: {info.DownloadId})");

        // Add to owners tracking
        if (!DownloadOwners.TryGetValue(info.DownloadId, out var owners))
        {
            owners = new HashSet<string>();
            DownloadOwners[info.DownloadId] = owners;
        }
        owners.Add(ownerId);

        // If already downloading, just add the owner and callback
        if (ActiveDownloads.Contains(info.DownloadId))
        {
            if (Manager.IsDebugEnabled)
                BetterContentLoadingMod.Logger.Msg($"Already downloading {info.DownloadId}, added owner {ownerId}");
            return;
        }

        DownloadInfo downloadInfo = info;
        var existingIndex = Queue.FindIndex(x => x.Info.DownloadId == downloadInfo.DownloadId);
        if (existingIndex >= 0)
        {
            // Update priority if needed based on new owner
            var newPriority = RecalculatePriority(info.DownloadId);
            Queue[existingIndex] = new QueueItem(info, newPriority, onComplete);
            SortQueue();
            return;
        }

        Queue.Add(new QueueItem(info, priority, onComplete));
        SortQueue();
        TryStartNextDownload();
    }

    public void RemoveOwner(string downloadId, string ownerId)
    {
        if (!DownloadOwners.TryGetValue(downloadId, out var owners))
            return;

        owners.Remove(ownerId);

        if (owners.Count == 0)
        {
            // No more owners, cancel the download
            DownloadOwners.Remove(downloadId);
            CancelDownload(downloadId);
        }
        else if (!ActiveDownloads.Contains(downloadId))
        {
            // Still has owners and is queued, recalculate priority
            var existingIndex = Queue.FindIndex(x => x.Info.DownloadId == downloadId);
            if (existingIndex >= 0)
            {
                var item = Queue[existingIndex];
                var newPriority = RecalculatePriority(downloadId);
                Queue[existingIndex] = new QueueItem(item.Info, newPriority, item.OnComplete);
                SortQueue();
            }
        }
    }

    protected virtual async void TryStartNextDownload()
    {
        try
        {
            if (Queue.Count == 0) return;

            await DownloadSemaphore.WaitAsync();

            if (Queue.Count > 0)
            {
                var item = Queue[0];
                Queue.RemoveAt(0);

                // Double check we still have owners before starting download
                if (!DownloadOwners.TryGetValue(item.Info.DownloadId, out var owners) || owners.Count == 0)
                {
                    DownloadSemaphore.Release();
                    TryStartNextDownload();
                    return;
                }

                ActiveDownloads.Add(item.Info.DownloadId);

                try
                {
                    await ProcessDownload(item.Info);
                    BetterContentLoadingMod.Logger.Msg($"Download completed for {item.Info.DownloadId}");
                    
                    // Notify all owners of completion
                    if (DownloadOwners.TryGetValue(item.Info.DownloadId, out owners))
                    {
                        foreach (var owner in owners)
                        {
                            item.OnComplete?.Invoke(item.Info.DownloadId, owner);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Manager.IsDebugEnabled)
                        BetterContentLoadingMod.Logger.Error($"Download failed for {item.Info.DownloadId}: {ex}");
                }
                finally
                {
                    ActiveDownloads.Remove(item.Info.DownloadId);
                    DownloadSemaphore.Release();
                    TryStartNextDownload();
                }
            }
            else
            {
                DownloadSemaphore.Release();
            }
        }
        catch (Exception e)
        {
            BetterContentLoadingMod.Logger.Error($"Error in TryStartNextDownload: {e}");
        }
    }

    protected virtual async Task ProcessDownload(DownloadInfo info)
    {
        bool success = await Manager.ProcessDownload(info);

        if (!success)
            throw new Exception($"Failed to download {info.AssetId}");
    }

    protected abstract void OnDownloadProgress(string downloadId, float progress);
    
    protected abstract float RecalculatePriority(string downloadId);

    protected virtual void SortQueue()
    {
        Queue.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    protected virtual void CancelDownload(string downloadId)
    {
        Queue.RemoveAll(x => x.Info.DownloadId == downloadId);
        if (ActiveDownloads.Remove(downloadId)) DownloadSemaphore.Release();
    }
}