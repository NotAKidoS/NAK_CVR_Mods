using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;
using UnityEngine;

namespace NAK.Stickers.Patches;

internal static class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.GetCurrentPropSelectionMode))]
    private static void Postfix_PlayerSetup_GetCurrentPropSelectionMode(ref PlayerSetup.PropSelectionMode __result)
    {
        if (StickerSystem.Instance.IsInStickerMode) __result = (PlayerSetup.PropSelectionMode)4;
    }
}

internal static class ControllerRayPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.HandlePropSpawn))]
    private static void Prefix_ControllerRay_HandlePropSpawn(ref ControllerRay __instance)
    {
        if (!StickerSystem.Instance.IsInStickerMode) 
            return;

        if (__instance._gripDown) StickerSystem.Instance.IsInStickerMode = false;
        if (__instance._hitUIInternal || !__instance._interactDown) 
            return;
        
        StickerSystem.Instance.PlaceStickerFromTransform(__instance.rayDirectionTransform);
    }
}