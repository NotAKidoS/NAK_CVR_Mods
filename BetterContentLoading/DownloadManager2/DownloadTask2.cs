namespace NAK.BetterContentLoading;

public class DownloadTask2
{
    public DownloadInfo Info { get; }
    public DownloadTaskStatus Status { get; set; }
    public DownloadTaskType Type { get; }
    
    public float BasePriority { get; set; }
    public float CurrentPriority => BasePriority * (1 + Progress / 100f);
    
    public long BytesRead { get; set; }
    public int Progress { get; set; }
    
    public string CachePath { get; }
    public Dictionary<string, string> Targets { get; } // Key: targetId (playerId/instanceId), Value: spawnerId
    public bool LoadOnComplete { get; } // For worlds only

    public DownloadTask2(
        DownloadInfo info,
        string cachePath,
        DownloadTaskType type,
        bool loadOnComplete = false)
    {
        Info = info;
        CachePath = cachePath;
        Type = type;
        LoadOnComplete = loadOnComplete;
        Targets = new Dictionary<string, string>();
        Status = DownloadTaskStatus.Queued;
    }

    public void AddTarget(string targetId, string spawnerId = null)
    {
        if (Type == DownloadTaskType.World && Targets.Count > 0)
            throw new InvalidOperationException("World downloads cannot have multiple targets");
            
        Targets[targetId] = spawnerId;
    }

    public void RemoveTarget(string targetId)
    {
        Targets.Remove(targetId);
    }
}

public enum DownloadTaskType
{
    Avatar,
    Prop,
    World
}

public enum DownloadTaskStatus
{
    Queued,
    Downloading,
    Paused,
    Complete,
    Failed,
    Cancelled
}