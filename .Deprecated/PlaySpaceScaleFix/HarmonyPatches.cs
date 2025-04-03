using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using UnityEngine;

namespace NAK.PlaySpaceScaleFix.HarmonyPatches;

class PlayerSetupPatches
{
    /**

        Store vrCamera position before vrRig is scaled.
        Use new vrCamera position after vrRig is scaled to get an offset.

        Use offset on _PlayerLocal object to keep player in place.
        Calculate scale difference, use to scale the avatars local position to keep avatar in place.

    **/

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
        // DesktopVRSwitch might allow an offset other than 0,0,0 for the vrCamera in Desktop
        // Safest to just not run this patch if in Desktop, as Desktop doesn't have an offset at all anyways
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
            // This calculation done in PlayerSetup.CheckUpdateAvatarScaleToPlaySpaceRelation already, but using Initial scale.
            // We only need difference between last and current scale for scaling the localposition.
            Vector3 scaleDifferenceNotInitial = __instance.DivideVectors(__instance._avatar.transform.localScale, __instance.lastScale);
            __instance._avatar.transform.localPosition = Vector3.Scale(__instance._avatar.transform.localPosition, scaleDifferenceNotInitial);
        }
    }
}