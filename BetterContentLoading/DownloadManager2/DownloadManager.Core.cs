using System.Collections.Concurrent;
using ABI_RC.Core;
using ABI_RC.Core.IO;
using ABI_RC.Core.IO.AssetManagement;

namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    #region Singleton
    private static DownloadManager2 _instance;
    public static DownloadManager2 Instance => _instance ??= new DownloadManager2();
    #endregion

    #region Settings
    public bool IsDebugEnabled { get; set; } = true;
    public bool PrioritizeFriends { get; set; } = true;
    public bool PrioritizeDistance { get; set; } = true;
    public float PriorityDownloadDistance { get; set; } = 25f;
    public int MaxConcurrentDownloads { get; set; } = 5;
    public int MaxDownloadBandwidth { get; set; } // 100MB default
    private const int THROTTLE_THRESHOLD = 25 * 1024 * 1024; // 25MB threshold for throttling
    #endregion

    #region State
    private readonly ConcurrentDictionary<string, DownloadTask2> _activeDownloads;
    private readonly ConcurrentPriorityQueue<DownloadTask2, float> _queuedDownloads;
    private readonly ConcurrentDictionary<string, DownloadTask2> _completedDownloads;
    private readonly object _downloadLock = new();
    private long _bytesReadLastSecond;
    #endregion

    private DownloadManager2()
    {
        _activeDownloads = new ConcurrentDictionary<string, DownloadTask2>();
        _queuedDownloads = new ConcurrentPriorityQueue<DownloadTask2, float>();
        MaxDownloadBandwidth = 100 * 1024 * 1024;
        StartBandwidthMonitor();
    }
    
    public async Task<bool> QueueAvatarDownload(DownloadInfo info, string playerId)
    {
        if (await ValidateAndCheckCache(info))
            return true;

        DownloadTask2 task2 = GetOrCreateDownloadTask(info, DownloadTaskType.Avatar);
        task2.AddTarget(playerId);
        QueueDownload(task2);
        return false;
    }

    public async Task<bool> QueuePropDownload(DownloadInfo info, string instanceId, string spawnerId)
    {
        if (await ValidateAndCheckCache(info))
            return true;

        DownloadTask2 task2 = GetOrCreateDownloadTask(info, DownloadTaskType.Prop);
        task2.AddTarget(instanceId, spawnerId);
        QueueDownload(task2);
        return false;
    }

    public async Task<bool> QueueWorldDownload(DownloadInfo info, bool loadOnComplete)
    {
        if (await ValidateAndCheckCache(info))
            return true;

        DownloadTask2 task2 = GetOrCreateDownloadTask(info, DownloadTaskType.World, loadOnComplete);
        QueueDownload(task2);
        return false;
    }

    private async Task<bool> ValidateAndCheckCache(DownloadInfo info)
    {
        // Check if already cached and up to date
        if (await CacheManager.Instance.IsCachedFileUpToDate(
            info.AssetId,
            info.FileId,
            info.FileHash))
        {
            return true;
        }

        // Validate disk space
        var filePath = CacheManager.Instance.GetCachePath(info.AssetId, info.FileId);
        if (!CVRTools.HasEnoughDiskSpace(filePath, info.FileSize))
        {
            BetterContentLoadingMod.Logger.Error($"Not enough disk space to download {info.AssetId}");
            return false;
        }

        // Ensure cache directory exists
        CacheManager.Instance.EnsureCacheDirectoryExists(info.AssetId);
        return false;
    }

    private DownloadTask2 GetOrCreateDownloadTask(DownloadInfo info, DownloadTaskType type, bool loadOnComplete = false)
    {
        // Check if task already exists in active downloads
        if (_activeDownloads.TryGetValue(info.DownloadId, out var activeTask))
            return activeTask;

        // Check if task exists in queued downloads
        var queuedTask = _queuedDownloads.TryFind(t => t.Info.DownloadId == info.DownloadId);
        if (queuedTask != null)
            return queuedTask;

        // Create new task
        var cachePath = CacheManager.Instance.GetCachePath(info.AssetId, info.FileId);
        return new DownloadTask2(info, cachePath, type, loadOnComplete);
    }
    
    public bool TryFindTask(string downloadId, out DownloadTask2 task2)
    {
        return _activeDownloads.TryGetValue(downloadId, out task2) || 
               _completedDownloads.TryGetValue(downloadId, out task2) ||
               TryFindQueuedTask(downloadId, out task2);
    }

    private bool TryFindQueuedTask(string downloadId, out DownloadTask2 task2)
    {
        task2 = _queuedDownloads.UnorderedItems
            .FirstOrDefault(x => x.Element.Info.DownloadId == downloadId).Element;
        return task2 != null;
    }
}