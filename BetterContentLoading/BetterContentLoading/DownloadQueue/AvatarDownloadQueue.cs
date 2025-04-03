namespace NAK.BetterContentLoading.Queue;

public class AvatarDownloadQueue : ContentDownloadQueueBase
{
    public AvatarDownloadQueue(BetterDownloadManager manager) : base(manager, 3) { }

    public void QueueDownload(in DownloadInfo info, string playerId, Action<string, string> onComplete = null)
    {
        float priority = CalculateAvatarPriority(in info, playerId);
        QueueDownload(in info, playerId, priority, onComplete);
    }

    protected override async Task ProcessDownload(DownloadInfo info)
    {
        await base.ProcessDownload(info);
    }
    
    protected override void OnDownloadProgress(string downloadId, float progress)
    {
        if (DownloadOwners.TryGetValue(downloadId, out var owners))
        {
            foreach (var playerId in owners)
            {
                if (BetterDownloadManager.IsPlayerLocal(playerId))
                {
                    // Update loading progress on local player
                    BetterContentLoadingMod.Logger.Msg($"Progress for local player ({playerId}): {progress:P}");
                    continue;
                }
                if (BetterDownloadManager.TryGetPlayerEntity(playerId, out var player))
                {
                    // Update loading progress on avatar controller if needed
                    BetterContentLoadingMod.Logger.Msg($"Progress for {player.Username} ({playerId}): {progress:P}");
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
        foreach (var playerId in owners)
        {
            var priority = CalculateAvatarPriority(downloadInfo.Value, playerId);
            lowestPriority = Math.Min(lowestPriority, priority);
        }

        return lowestPriority;
    }

    private float CalculateAvatarPriority(in DownloadInfo info, string playerId)
    {
        float priority = info.FileSize;

        if (BetterDownloadManager.IsPlayerLocal(playerId))
            return 0f;

        if (!BetterDownloadManager.TryGetPlayerEntity(playerId, out var player))
            return priority;

        if (Manager.PrioritizeFriends && BetterDownloadManager.IsPlayerFriend(playerId))
            priority *= 0.5f;

        if (Manager.PrioritizeDistance && Manager.IsPlayerWithinPriorityDistance(player))
            priority *= 0.75f;

        return priority;
    }
}