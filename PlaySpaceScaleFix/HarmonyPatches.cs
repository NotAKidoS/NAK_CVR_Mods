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
        
        Vector3 offset = newPosition - __state;

        // Offset _PlayerLocal to keep player in place
        __instance.transform.position -= offset;

        // TODO: Figure out why VRIK is wonky still
        // PlayerSetup runs after VRIK solving?? Fuck
        /**
        if (IKSystem.vrik != null)
        {
            IKSystem.vrik.transform.position += offset;
            IKSystem.vrik.solver.Reset();
            IKSystem.vrik.solver.AddPlatformMotion(offset, Quaternion.identity, __instance.transform.position);
        }
        **/
    }
}