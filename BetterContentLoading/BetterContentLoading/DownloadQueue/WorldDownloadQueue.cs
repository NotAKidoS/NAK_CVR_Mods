namespace NAK.BetterContentLoading.Queue;

public class WorldDownloadQueue : ContentDownloadQueueBase
{
    private readonly Queue<(DownloadInfo Info, bool JoinOnComplete, bool IsHomeRequest)> _backgroundQueue = new();
    private bool _isProcessingPriorityDownload;

    public WorldDownloadQueue(BetterDownloadManager manager) : base(manager, 1) { }

    public void QueueDownload(in DownloadInfo info, bool joinOnComplete, bool isHomeRequest, Action<string, string> onComplete = null)
    {
        if (joinOnComplete || isHomeRequest)
        {
            // Priority download - clear queue and download immediately
            Queue.Clear();
            _isProcessingPriorityDownload = true;
            QueueDownload(in info, info.DownloadId, 0f, onComplete);
        }
        else
        {
            // Background download - add to background queue
            _backgroundQueue.Enqueue((info, false, false));
            if (!_isProcessingPriorityDownload)
                ProcessBackgroundQueue();
        }
    }

    protected override async Task ProcessDownload(DownloadInfo info)
    {
        await base.ProcessDownload(info);
        
        if (_isProcessingPriorityDownload)
        {
            _isProcessingPriorityDownload = false;
            ProcessBackgroundQueue();
        }
    }
    
    protected override void OnDownloadProgress(string downloadId, float progress)
    {
        BetterContentLoadingMod.Logger.Msg($"World download progress: {progress:P}");
    }

    protected override float RecalculatePriority(string downloadId)
    {
        // For worlds, priority is based on whether it's a priority download and file size
        var queueItem = Queue.Find(x => x.Info.DownloadId == downloadId);
        return queueItem.Priority;
    }

    private void ProcessBackgroundQueue()
    {
        while (_backgroundQueue.Count > 0)
        {
            (DownloadInfo info, var join, var home) = _backgroundQueue.Dequeue();
            QueueDownload(in info, info.DownloadId, info.FileSize, null);
        }
    }

    protected override void CancelDownload(string downloadId)
    {
        base.CancelDownload(downloadId);
        _backgroundQueue.Clear();
        _isProcessingPriorityDownload = false;
    }
}