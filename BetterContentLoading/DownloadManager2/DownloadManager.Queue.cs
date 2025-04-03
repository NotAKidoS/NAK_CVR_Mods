using ABI_RC.Core.IO;

namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    public void QueueDownload(DownloadTask2 newTask2)
    {
        if (_completedDownloads.TryGetValue(newTask2.Info.DownloadId, out var completedTask))
        {
            completedTask.AddPlayer(newTask2.PlayerIds.FirstOrDefault());
            return;
        }

        if (TryFindTask(newTask2.Info.DownloadId, out var existingTask))
        {
            existingTask.AddPlayer(newTask2.PlayerIds.FirstOrDefault());
            RecalculatePriority(existingTask);
            return;
        }

        float priority = CalculatePriority(newTask2);
        newTask2.CurrentPriority = priority;

        lock (_downloadLock)
        {
            if (_activeDownloads.Count < MaxConcurrentDownloads)
            {
                StartDownload(newTask2);
                return;
            }

            var lowestPriorityTask = _activeDownloads.Values
                .OrderByDescending(t => t.CurrentPriority * (1 + t.Progress / 100f))
                .LastOrDefault();

            if (lowestPriorityTask != null && priority < lowestPriorityTask.CurrentPriority)
            {
                PauseDownload(lowestPriorityTask);
                StartDownload(newTask2);
            }
            else
            {
                _queuedDownloads.Enqueue(newTask2, priority);
            }
        }
    }
    
    private void StartDownload(DownloadTask2 task2)
    {
        task2.Status = DownloadTaskStatus.Downloading;
        _activeDownloads.TryAdd(task2.Info.DownloadId, task2);
        Task.Run(() => ProcessDownload(task2));
    }

    private void PauseDownload(DownloadTask2 task2)
    {
        task2.Status = DownloadTaskStatus.Queued;
        _activeDownloads.TryRemove(task2.Info.DownloadId, out _);
        _queuedDownloads.Enqueue(task2, task2.CurrentPriority);
    }
    


    
    private void RecalculatePriority(DownloadTask2 task2)
    {
        var newPriority = CalculatePriority(task2);
        if (Math.Abs(newPriority - task2.CurrentPriority) < float.Epsilon) 
            return;

        task2.CurrentPriority = newPriority;
        
        if (task2.Status == DownloadTaskStatus.Queued || task2.Status == DownloadTaskStatus.Paused)
        {
            // Re-enqueue with new priority
            _queuedDownloads.UpdatePriority(task2, newPriority);
        }
    }
}