using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using HarmonyLib;
using UnityEngine;

namespace NAK.PlaySpaceScaleFix.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetPlaySpaceScale))]
    private static void Prefix_PlayerSetup_SetPlaySpaceScale(ref PlayerSetup __instance, ref Vector3 __state)
    {
        __state = __instance.vrCamera.transform.position;
        __state.y = __instance.transform.position.y;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetPlaySpaceScale))]
    private static void Postfix_PlayerSetup_SetPlaySpaceScale(ref PlayerSetup __instance, ref Vector3 __state)
    {
        if (!PlaySpaceScaleFix.EntryEnabled.Value)
            return;

        Vector3 newPosition = __instance.vrCamera.transform.position;
        newPosition.y = __instance.transform.position.y;
        
        Vector3 offset = __state + newPosition;

        // Offset _PlayerLocal to keep player in place
        __instance.transform.position += offset;

        // TODO: Figure out why VRIK is wonky still
        if (IKSystem.vrik != null)
        {
            IKSystem.vrik.solver.locomotion.AddDeltaPosition(offset);
            IKSystem.vrik.solver.raycastOriginPelvis += offset;
            IKSystem.vrik.transform.position += offset;
        }
    }
}