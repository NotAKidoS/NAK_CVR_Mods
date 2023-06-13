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

        if (____movementSystem.disableCameraControl && !ignore)
            return;

        if (___headBobbingLevel != 2)
            return;

        Transform viewpointTransform = __instance._viewPoint.pointer.transform;
        if (viewpointTransform != null)
        {
            __instance.desktopCamera.transform.position = viewpointTransform.position;
        }
        
        /**
            desktopCameraRig -> desktopCamera
            desktopCameraRig is parent of desktopCamera. 

            desktopCamera rotates, so it pivots in place.
            desktopCameraRig is moved to head bone, local position of camera is viewpoint offset when standing

            if rig was moving position & rotation, this would work
            but because rig handles position and camera handles rotation, camera pivots in place instead of at correct point
            which is gross

        **/
    }
}
