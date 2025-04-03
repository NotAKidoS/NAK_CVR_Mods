namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    private void StartBandwidthMonitor()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                Interlocked.Exchange(ref _bytesReadLastSecond, 0);
                Interlocked.Exchange(ref _completedDownloads, 0);
            }
        });
    }

    private int ComputeUsableBandwidthPerDownload()
    {
        var activeCount = _activeDownloads.Count;
        if (activeCount == 0) return MaxDownloadBandwidth;
        return MaxDownloadBandwidth / activeCount;
    }
}