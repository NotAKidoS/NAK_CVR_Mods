using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using NAK.RelativeSync.Components;
using UnityEngine;

namespace NAK.RelativeSync;

public static class RelativeSyncManager
{
    public const int NoTarget = -1;
    
    public static readonly Dictionary<int, RelativeSyncMarker> RelativeSyncTransforms = new();
    public static readonly Dictionary<string, RelativeSyncController> RelativeSyncControllers = new();

    public static void ApplyRelativeSync(string userId, int target, Vector3 position, Vector3 rotation)
    {
        if (!RelativeSyncControllers.TryGetValue(userId, out RelativeSyncController controller))
            if (CVRPlayerManager.Instance.GetPlayerPuppetMaster(userId, out PuppetMaster pm))
                controller = pm.AddComponentIfMissing<RelativeSyncController>();

        if (controller == null)
        {
            RelativeSyncMod.Logger.Error($"Failed to apply relative sync for user {userId}");
            return;
        }
        
        // find target transform
        RelativeSyncMarker syncMarker = null;
        if (target != NoTarget) RelativeSyncTransforms.TryGetValue(target, out syncMarker);
        
        controller.SetRelativePositions(position, rotation);
        controller.SetRelativeSyncMarker(syncMarker);
    }
}