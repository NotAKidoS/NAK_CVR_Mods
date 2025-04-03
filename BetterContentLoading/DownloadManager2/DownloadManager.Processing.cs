using System.Diagnostics;
using System.Net.Http.Headers;
using ABI_RC.Core.IO;

namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    private async Task ProcessDownload(DownloadTask2 task2)
    {
        using var client = new HttpClient();
        
        // Set up resume headers if we have a resume token
        if (!string.IsNullOrEmpty(task2.ResumeToken))
        {
            client.DefaultRequestHeaders.Range = new RangeHeaderValue(task2.BytesRead, null);
        }

        using var response = await client.GetAsync(task2.Info.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
        using var dataStream = await response.Content.ReadAsStreamAsync();
        
        // Open file in append mode if resuming, otherwise create new
        using var fileStream = new FileStream(
            task2.TargetPath, 
            string.IsNullOrEmpty(task2.ResumeToken) ? FileMode.Create : FileMode.Append, 
            FileAccess.Write);

        bool isEligibleForThrottle = task2.Info.FileSize > THROTTLE_THRESHOLD;
        var stopwatch = new Stopwatch();
        
        int bytesRead;
        do
        {
            if (task2.Status == DownloadTaskStatus.Paused)
            {
                task2.ResumeToken = GenerateResumeToken(task2);
                await fileStream.FlushAsync();
                return;
            }
            
            if (task2.Status != DownloadTaskStatus.Downloading)
            {
                HandleCancellation(task2, dataStream, fileStream);
                return;
            }

            stopwatch.Restart();
            var lengthToRead = isEligibleForThrottle ? 
                ComputeUsableBandwidthPerDownload() : 
                16384;

            var buffer = new byte[lengthToRead];
            bytesRead = await dataStream.ReadAsync(buffer, 0, lengthToRead);

            if (isEligibleForThrottle)
                Interlocked.Add(ref _bytesReadLastSecond, bytesRead);

            await fileStream.WriteAsync(buffer, 0, bytesRead);
            UpdateProgress(task2, bytesRead);

        } while (bytesRead > 0);

        CompleteDownload(task2);
    }

    private string GenerateResumeToken(DownloadTask2 task2)
    {
        // Generate a unique token that includes file position and hash
        return $"{task2.BytesRead}:{DateTime.UtcNow.Ticks}";
    }
    
    private async Task FinalizeDownload(DownloadTask2 task2)
    {
        var tempPath = task2.CachePath + ".tmp";
        
        try
        {
            // Decrypt the file if needed
            if (task2.Info.EncryptionAlgorithm != 0)
            {
                await DecryptFile(tempPath, task2.CachePath, task2.Info);
                File.Delete(tempPath);
            }
            else
            {
                File.Move(tempPath, task2.CachePath);
            }

            CompleteDownload(task2);
        }
        catch (Exception ex)
        {
            // _logger.Error($"Failed to finalize download for {task.Info.AssetId}: {ex.Message}");
            task2.Status = DownloadTaskStatus.Failed;
            File.Delete(tempPath);
            _activeDownloads.TryRemove(task2.Info.DownloadId, out _);
        }
    }

    private async Task DecryptFile(string sourcePath, string targetPath, DownloadInfo info)
    {
        // Implementation of file decryption based on EncryptionAlgorithm
        // This would use the FileKey from the DownloadInfo
        throw new NotImplementedException("File decryption not implemented");
    }
    
    private void HandleCancellation(DownloadTask2 task2, Stream dataStream, Stream fileStream)
    {
        if (task2.Status != DownloadTaskStatus.Failed)
            task2.Status = DownloadTaskStatus.Cancelled;

        dataStream.Close();
        fileStream.Close();

        task2.Progress = 0;
        task2.BytesRead = 0;

        _activeDownloads.TryRemove(task2.Info.DownloadId, out _);
    }

    private void UpdateProgress(DownloadTask2 task2, int bytesRead)
    {
        task2.BytesRead += bytesRead;
        task2.Progress = Math.Clamp(
            (int)(((float)task2.BytesRead / task2.Info.FileSize) * 100f),
            0, 100);
    }

    private void CompleteDownload(DownloadTask2 task2)
    {
        task2.Status = DownloadTaskStatus.Complete;
        task2.Progress = 100;
        _activeDownloads.TryRemove(task2.Info.DownloadId, out _);
        _completedDownloads.TryAdd(task2.Info.DownloadId, task2);
        
        lock (_downloadLock)
        {
            if (_queuedDownloads.TryDequeue(out var nextTask, out _))
            {
                StartDownload(nextTask);
            }
        }
    }
}