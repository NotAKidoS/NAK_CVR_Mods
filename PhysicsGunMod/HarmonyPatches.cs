using ABI_RC.Systems.InputManagement;
using HarmonyLib;
using NAK.PhysicsGunMod.Components;
using UnityEngine;

namespace NAK.PhysicsGunMod.HarmonyPatches;

internal static class CVRInputManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), nameof(CVRInputManager.Update))]
    private static void Postfix_CVRInputManager_Update(ref CVRInputManager __instance)
    {
        if (PhysicsGunInteractionBehavior.Instance == null)
            return;
        
        if (PhysicsGunInteractionBehavior.Instance.UserRotation)
            __instance.lookVector = Vector2.zero;
    }
}
