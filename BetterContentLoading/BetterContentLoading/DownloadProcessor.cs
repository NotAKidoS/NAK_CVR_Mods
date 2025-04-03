using System.Net.Http;
using ABI_RC.Core;
using ABI_RC.Core.IO.AssetManagement;

namespace NAK.BetterContentLoading;

public class DownloadProcessor
{
    private readonly HttpClient _client = new();
    private readonly SemaphoreSlim _bandwidthSemaphore = new(1);
    private long _bytesReadLastSecond;
        
    private int _maxDownloadBandwidth = 10 * 1024 * 1024; // Default 10MB/s
    private const int MinBufferSize = 16384; // 16KB min buffer
    private const long ThrottleThreshold = 25 * 1024 * 1024; // 25MB threshold for throttling
    private const long KnownSizeDifference = 1000; // API reported size is 1000 bytes larger than actual content

    public int MaxDownloadBandwidth
    {
        get => _maxDownloadBandwidth;
        set => _maxDownloadBandwidth = Math.Max(1024 * 1024, value);
    }

    private int ActiveDownloads { get; set; }
    private int CurrentBandwidthPerDownload => MaxDownloadBandwidth / Math.Max(1, ActiveDownloads);

    public int GetProgress(string downloadId) => _downloadProgress.GetValueOrDefault(downloadId, 0);
    private readonly Dictionary<string, int> _downloadProgress = new();

    public async Task<bool> ProcessDownload(DownloadInfo downloadInfo)
    {
        try
        {
            if (await CacheManager.Instance.IsCachedFileUpToDate(
                downloadInfo.AssetId, 
                downloadInfo.FileId, 
                downloadInfo.FileHash))
                return true;

            var filePath = CacheManager.Instance.GetCachePath(downloadInfo.AssetId, downloadInfo.FileId);
            if (!CVRTools.HasEnoughDiskSpace(filePath, downloadInfo.FileSize))
            {
                BetterContentLoadingMod.Logger.Error($"Not enough disk space to download {downloadInfo.AssetId}");
                return false;
            }

            CacheManager.Instance.EnsureCacheDirectoryExists(downloadInfo.AssetId);

            bool success = false;
            Exception lastException = null;

            for (int attempt = 0; attempt <= 1; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(1000);

                    success = await DownloadWithBandwidthLimit(downloadInfo, filePath);
                    if (success) break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (!success && lastException != null)
                BetterContentLoadingMod.Logger.Error($"Failed to download {downloadInfo.AssetId}: {lastException}");

            _downloadProgress.Remove(downloadInfo.DownloadId);
            return success;
        }
        catch (Exception ex)
        {
            BetterContentLoadingMod.Logger.Error($"Error processing download for {downloadInfo.AssetId}: {ex}");
            _downloadProgress.Remove(downloadInfo.DownloadId);
            return false;
        }
    }

    private async Task<bool> DownloadWithBandwidthLimit(DownloadInfo downloadInfo, string filePath)
    {
        await _bandwidthSemaphore.WaitAsync();
        try { ActiveDownloads++; }
        finally { _bandwidthSemaphore.Release(); }

        string tempFilePath = filePath + ".download";
        try
        {
            using var response = await _client.GetAsync(downloadInfo.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return false;

            var expectedContentSize = downloadInfo.FileSize - KnownSizeDifference;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Open(tempFilePath, FileMode.Create);

            var isEligibleForThrottle = downloadInfo.FileSize >= ThrottleThreshold;
            var totalBytesRead = 0L;
            byte[] buffer = null;

            while (true)
            {
                var lengthToRead = isEligibleForThrottle ? MinBufferSize : CurrentBandwidthPerDownload;
                if (buffer == null || lengthToRead != buffer.Length)
                    buffer = new byte[lengthToRead];

                var bytesRead = await stream.ReadAsync(buffer, 0, lengthToRead);
                if (bytesRead == 0) break;

                if (isEligibleForThrottle)
                    Interlocked.Add(ref _bytesReadLastSecond, bytesRead);

                fileStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                var progress = (int)(((float)totalBytesRead / expectedContentSize) * 100f);
                _downloadProgress[downloadInfo.DownloadId] = Math.Clamp(progress, 0, 100);
            }

            fileStream.Flush();
            
            var fileInfo = new FileInfo(tempFilePath);
            if (fileInfo.Length != expectedContentSize)
                return false;

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tempFilePath, filePath);

            CacheManager.Instance.QueuePrune();
            return true;
        }
        finally
        {
            await _bandwidthSemaphore.WaitAsync();
            try { ActiveDownloads--; }
            finally { _bandwidthSemaphore.Release(); }

            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
            catch 
            {
                // Ignore cleanup errors
            }
        }
    }
}