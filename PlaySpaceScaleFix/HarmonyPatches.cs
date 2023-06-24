using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using UnityEngine;

namespace NAK.PlaySpaceScaleFix.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetPlaySpaceScale))]
    static void Prefix_PlayerSetup_SetPlaySpaceScale(ref PlayerSetup __instance, ref Vector3 __state)
    {
        __state = __instance.vrCamera.transform.position;
        __state.y = __instance.transform.position.y;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetPlaySpaceScale))]
    static void Postfix_PlayerSetup_SetPlaySpaceScale(ref PlayerSetup __instance, ref Vector3 __state)
    {
        if (!PlaySpaceScaleFix.EntryEnabled.Value || !MetaPort.Instance.isUsingVr)
            return;

        Vector3 newPosition = __instance.vrCamera.transform.position;
        newPosition.y = __instance.transform.position.y;

        Vector3 offset = newPosition - __state;

        // Offset _PlayerLocal to keep player in place
        __instance.transform.position -= offset;

        // Scale avatar local position to keep avatar in place
        if (__instance._avatar != null)
        {
            Vector3 scaleDifference = __instance.DivideVectors(__instance._avatar.transform.localScale, __instance.lastScale);
            __instance._avatar.transform.localPosition = Vector3.Scale(__instance._avatar.transform.localPosition, scaleDifference);
        }
    }
}