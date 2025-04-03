using ABI_RC.Core.Player;

namespace NAK.BetterContentLoading;

public partial class DownloadManager
{
    private float CalculatePriority(DownloadTask task)
    {
        return task.Type switch
        {
            DownloadTaskType.Avatar => CalculateAvatarPriority(task),
            // DownloadTaskType.Prop => CalculatePropPriority(task2),
            // DownloadTaskType.World => CalculateWorldPriority(task2),
            _ => task.Info.FileSize
        };
    }
    
    private float CalculateAvatarPriority(DownloadTask task)
    {
        float priority = task.Info.FileSize;
        
        foreach (string target in task.InstantiationTargets)
        {
            if (IsPlayerLocal(target)) return 0f;

            if (!TryGetPlayerEntity(target, out CVRPlayerEntity player))
                return priority;
            
            if (PrioritizeFriends && IsPlayerFriend(target))
                priority *= 0.5f;
            
            if (PrioritizeDistance && IsPlayerWithinPriorityDistance(player))
                priority *= 0.75f;
        }
        
        return priority;
    }
}