using ABI_RC.Core.Player;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using UnityEngine;

namespace NAK.DesktopCameraFix.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.HandleDesktopCameraPosition))]
    public static void Postfix_PlayerSetup_HandleDesktopCameraPosition(bool ignore, ref PlayerSetup __instance, ref MovementSystem ____movementSystem, ref int ___headBobbingLevel)
    {
        if (!DesktopCameraFix.EntryEnabled.Value)
            return;

        // this would be much simplier if I bothered with transpilers
        if (____movementSystem.disableCameraControl && !ignore)
            return;

        if (___headBobbingLevel != 2)
            return;

        Transform viewpointTransform = __instance._viewPoint.pointer.transform;
        if (viewpointTransform != null)
        {
            __instance.desktopCamera.transform.position = viewpointTransform.position;
        }
    }
}
