using ABI_RC.Core.Util;

namespace NAK.BetterContentLoading.Queue;

public class PropDownloadQueue : ContentDownloadQueueBase
{
    private readonly Dictionary<string, string> _ownerToSpawner = new(); // InstanceId -> SpawnerId

    public PropDownloadQueue(BetterDownloadManager manager) : base(manager, 2) { }

    public void QueueDownload(in DownloadInfo info, string instanceId, string spawnerId, Action<string, string> onComplete = null)
    {
        _ownerToSpawner[instanceId] = spawnerId;
        float priority = CalculatePropPriority(in info, instanceId, spawnerId);
        QueueDownload(in info, instanceId, priority, onComplete);
    }

    protected override async Task ProcessDownload(DownloadInfo info)
    {
        await base.ProcessDownload(info);
    }
    
    protected override void OnDownloadProgress(string downloadId, float progress)
    {
        if (DownloadOwners.TryGetValue(downloadId, out var owners))
        {
            foreach (var instanceId in owners)
            {
                if (BetterDownloadManager.TryGetPropData(instanceId, out CVRSyncHelper.PropData prop))
                {
                    BetterContentLoadingMod.Logger.Msg($"Progress for {prop.InstanceId}: {progress:P}");
                }
            }
        }
    }

    protected override float RecalculatePriority(string downloadId)
    {
        if (!DownloadOwners.TryGetValue(downloadId, out var owners))
            return float.MaxValue;

        float lowestPriority = float.MaxValue;
        DownloadInfo? downloadInfo = null;

        // Find the queue item to get the DownloadInfo
        var queueItem = Queue.Find(x => x.Info.DownloadId == downloadId);
        if (queueItem.Info.AssetId != null)
            downloadInfo = queueItem.Info;

        if (downloadInfo == null)
            return lowestPriority;

        // Calculate priority for each owner and use the lowest (highest priority) value
        foreach (var instanceId in owners)
        {
            if (_ownerToSpawner.TryGetValue(instanceId, out var spawnerId))
            {
                var priority = CalculatePropPriority(downloadInfo.Value, instanceId, spawnerId);
                lowestPriority = Math.Min(lowestPriority, priority);
            }
        }

        return lowestPriority;
    }

    private float CalculatePropPriority(in DownloadInfo info, string instanceId, string spawnerId)
    {
        float priority = info.FileSize;

        if (!BetterDownloadManager.TryGetPropData(instanceId, out var prop))
            return priority;

        if (Manager.PrioritizeFriends && BetterDownloadManager.IsPlayerFriend(spawnerId))
            priority *= 0.5f;

        if (Manager.PrioritizeDistance && Manager.IsPropWithinPriorityDistance(prop))
            priority *= 0.75f;

        return priority;
    }
}