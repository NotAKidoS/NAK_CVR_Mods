namespace NAK.BetterContentLoading;

public partial class DownloadTask
{
    public DownloadInfo Info { get; set; }
    public DownloadTaskStatus Status { get; set; }
    public DownloadTaskType Type { get; set; }
    
    public float BasePriority { get; set; }
    public float CurrentPriority => BasePriority * (1 + Progress / 100f);
    
    public long BytesRead { get; set; }
    public int Progress { get; set; }
    
    /// The avatar/prop instances that wish to utilize this bundle.
    public List<string> InstantiationTargets { get; } = new();

    public void AddInstantiationTarget(string target)
    {
        if (InstantiationTargets.Contains(target))
            return;
        
        InstantiationTargets.Add(target);
    }
    
    public void RemoveInstantiationTarget(string target)
    {
        if (!InstantiationTargets.Contains(target))
            return;
        
        InstantiationTargets.Remove(target);
    }
}