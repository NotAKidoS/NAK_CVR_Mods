using ABI_RC.Core;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using HarmonyLib;
using UnityEngine;

namespace NAK.AvatarQueueSystemTweaks.Patches;

// internal static class CVRObjectLoaderPatches
// {
//     private static readonly YieldInstruction _yieldInstruction = new WaitForSeconds(0.2f);
//     
//     [HarmonyPostfix]
//     [HarmonyPatch(typeof(CVRObjectLoader), nameof(CVRObjectLoader.InstantiateAvatarFromBundle))]
//     [HarmonyPatch(typeof(CVRObjectLoader), nameof(CVRObjectLoader.InstantiateSpawnableFromBundle))]
//     //[HarmonyPatch(typeof(CVRObjectLoader), nameof(CVRObjectLoader.InstantiateAvatarFromExistingPrefab))]
//     //[HarmonyPatch(typeof(CVRObjectLoader), nameof(CVRObjectLoader.InstantiateSpawnableFromExistingPrefab))]
//     private static IEnumerator MyWrapper(IEnumerator result)
//     {
//         while (result.MoveNext())
//             yield return result.Current;
//         
//         if (AvatarQueueSystemTweaksMod.EntryChokeInstantiation.Value)
//             yield return _yieldInstruction;
//     }
// }

internal static class AvatarQueueSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AvatarQueueSystem.CoroutineHandle), nameof(AvatarQueueSystem.CoroutineHandle.RunManagedCoroutine))]
    private static bool Prefix_AvatarQueueSystem_CoroutineHandle_RunManagedCoroutine(AvatarQueueSystem.CoroutineHandle __instance)
    {
        AvatarQueueSystem.Instance.jobIsActive = true;
        
        // difference from original: doesn't immediately call CheckAvailability on same frame of completion
        
        __instance._activeCoroutine = AvatarQueueSystem.Instance.StartCoroutine(CoroutineUtil.RunThrowingIterator(__instance._function, delegate(Exception exception)
        {
            CommonTools.Log(CommonTools.LogLevelType_t.Error, string.Format("[CVRGame => {0}] Avatar loading job has failed!\n{1}", __instance.GetType().Name, exception), "A0108");
            __instance.CleanCurrentJob();
            var onException = __instance._onException;
            onException?.Invoke(exception);
        }, delegate
        {
            CommonTools.Log(CommonTools.LogLevelType_t.Info, "[CVRGame => " + __instance.GetType().Name + "] Avatar loading job has completed successfully.", "A0107");
            __instance.CleanCurrentJob();
            Action onSuccess = __instance._onSuccess;
            onSuccess?.Invoke();
        }));

        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AvatarQueueSystem), nameof(AvatarQueueSystem.CheckAvailability))]
    private static bool Prefix_AvatarQueueSystem_CheckAvailability(AvatarQueueSystem __instance)
    {
        if (__instance.JobIsActive 
            || __instance.activeCoroutines.Count == 0)
            return false;
        
        bool prioritizeSelf = AvatarQueueSystemTweaksMod.EntryPrioritizeSelf.Value;
        bool prioritizeFriends = AvatarQueueSystemTweaksMod.EntryPrioritizeFriends.Value;
        bool loadByDistance = AvatarQueueSystemTweaksMod.EntryLoadByDistance.Value;

        bool foundFriend = false;
        float nearestDistance = float.MaxValue;
        AvatarQueueSystem.CoroutineHandle nextCoroutine = null;
        Vector3 playerPosition = PlayerSetup.Instance.activeCam.transform.position;

        foreach (AvatarQueueSystem.CoroutineHandle coroutine in __instance.activeCoroutines)
        {
            if (prioritizeSelf && coroutine.player == "_PLAYERLOCAL") // prioritize local player if setting is enabled
            {
                nextCoroutine = coroutine;
                break;
            }

            CVRPlayerManager.Instance.GetPlayerPuppetMaster(coroutine.player, out PuppetMaster puppetMaster);
            if (puppetMaster == null)
                continue;
            
            if (prioritizeFriends)
            {
                switch (foundFriend)
                {
                    // attempt find first friend
                    case false when Friends.FriendsWith(coroutine.player):
                        foundFriend = true;
                        nearestDistance = float.MaxValue;
                        nextCoroutine = coroutine;
                        if (!loadByDistance) break; // no reason to continue loop
                        continue;
                    // found a friend, filtering will now only respect friends for this iteration
                    case true when !Friends.FriendsWith(coroutine.player):
                        continue;
                }
            }
            
            // filtering by distance
            if (!loadByDistance) continue;
            
            float distance = Vector3.Distance(playerPosition, puppetMaster.transform.position);
            if (!(distance < nearestDistance)) continue; // update nearest coroutine if closer
            
            nearestDistance = distance;
            nextCoroutine = coroutine;
        }

        nextCoroutine?.RunManagedCoroutine();
        return false;
    }
}