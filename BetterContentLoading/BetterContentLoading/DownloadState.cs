namespace NAK.BetterContentLoading;

public readonly struct DownloadState
{
    public readonly string DownloadId;
    public readonly long BytesRead;
    public readonly long TotalBytes;
    public readonly int ProgressPercent;
    public readonly float BytesPerSecond;

    public DownloadState(string downloadId, long bytesRead, long totalBytes, float bytesPerSecond)
    {
        DownloadId = downloadId;
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
        BytesPerSecond = bytesPerSecond;
        ProgressPercent = (int)(((float)bytesRead / totalBytes) * 100f);
    }
}

public interface IDownloadMonitor
{
    void OnDownloadProgress(DownloadState state);
    void OnDownloadStarted(string downloadId);
    void OnDownloadCompleted(string downloadId, bool success);
}