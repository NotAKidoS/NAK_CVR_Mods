﻿using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using NAK.RelativeSync.Components;
using UnityEngine;

namespace NAK.RelativeSync;

public static class RelativeSyncManager
{
    public const int NoTarget = -1;
    
    public static readonly Dictionary<int, RelativeSyncMarker> RelativeSyncTransforms = new();
    public static readonly Dictionary<string, RelativeSyncController> RelativeSyncControllers = new();
    public static readonly Dictionary<NetIKController, RelativeSyncController> NetIkControllersToRelativeSyncControllers = new();

    public static void ApplyRelativeSync(string userId, int target, Vector3 position, Vector3 rotation)
    {
        if (!RelativeSyncControllers.TryGetValue(userId, out RelativeSyncController controller))
        {
            if (CVRPlayerManager.Instance.GetPlayerPuppetMaster(userId, out PuppetMaster pm))
            {
                controller = pm.AddComponentIfMissing<RelativeSyncController>();
                RelativeSyncMod.Logger.Msg($"Found PuppetMaster for user {userId}. This user is now eligible for relative sync.");
            }
            else
            {
                RelativeSyncControllers.Add(userId, null); // add null controller to prevent future lookups
                RelativeSyncMod.Logger.Warning($"Failed to find PuppetMaster for user {userId}. This is likely because the user is blocked or has blocked you. This user will not be eligible for relative sync until next game restart.");
            }
        }
        
        if (controller == null) 
            return;
        
        // find target transform
        RelativeSyncMarker syncMarker = null;
        if (target != NoTarget) RelativeSyncTransforms.TryGetValue(target, out syncMarker);
        
        controller.SetRelativeSyncMarker(syncMarker);
        controller.SetRelativePositions(position, rotation);
    }
    
    public static void GetRelativeAvatarPositionsFromMarker(
        Animator avatarAnimator, Transform markerTransform,
        out Vector3 relativePosition, out Vector3 relativeRotation)
        // out Vector3 relativeHipPosition, out Vector3 relativeHipRotation)
    {
        Transform avatarTransform = avatarAnimator.transform;
        
        // because our syncing is retarded, we need to sync relative from the avatar root...
        Vector3 avatarRootPosition = avatarTransform.position; // PlayerSetup.Instance.GetPlayerPosition()
        Quaternion avatarRootRotation = avatarTransform.rotation; // PlayerSetup.Instance.GetPlayerRotation()
        
        relativePosition = markerTransform.InverseTransformPoint(avatarRootPosition);
        relativeRotation = (Quaternion.Inverse(markerTransform.rotation) * avatarRootRotation).eulerAngles;
        
        // Transform hipTrans = (avatarAnimator.avatar != null && avatarAnimator.isHuman) 
        //     ? avatarAnimator.GetBoneTransform(HumanBodyBones.Hips) : null;
        //
        // if (hipTrans == null)
        // {
        //     relativeHipPosition = Vector3.zero;
        //     relativeHipRotation = Vector3.zero;
        // }
        // else
        // {
        //     relativeHipPosition = markerTransform.InverseTransformPoint(hipTrans.position);
        //     relativeHipRotation = (Quaternion.Inverse(markerTransform.rotation) * hipTrans.rotation).eulerAngles;
        // }
    }
}