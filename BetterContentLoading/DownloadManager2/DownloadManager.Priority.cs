using ABI_RC.Core.IO;

namespace NAK.BetterContentLoading;

public partial class DownloadManager2
{
    private float CalculatePriority(DownloadTask2 task2)
    {
        return task2.Type switch
        {
            DownloadTaskType.Avatar => CalculateAvatarPriority(task2),
            DownloadTaskType.Prop => CalculatePropPriority(task2),
            DownloadTaskType.World => CalculateWorldPriority(task2),
            _ => task2.Info.FileSize
        };
    }

    private float CalculateAvatarPriority(DownloadTask2 task2)
    {
        float priority = task2.Info.FileSize;
        
        if (IsPlayerLocal(task2.PlayerId))
            return 0f;

        if (!TryGetPlayerEntity(task2.PlayerId, out var player))
            return priority;

        if (PrioritizeFriends && IsPlayerFriend(task2.PlayerId))
            priority *= 0.5f;

        if (PrioritizeDistance && IsPlayerWithinPriorityDistance(player))
            priority *= 0.75f;

        // Factor in download progress
        priority *= (1 + task2.Progress / 100f);

        return priority;
    }

    private float CalculatePropPriority(DownloadTask2 task2)
    {
        float priority = task2.Info.FileSize;
        
        if (IsPlayerLocal(task2.PlayerId))
            return 0f;
    }
}