using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
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

        StickerSystem.Instance.PlaceStickerFromControllerRay(__instance.rayDirectionTransform, __instance.hand, true); // preview

        if (__instance._gripDown) StickerSystem.Instance.IsInStickerMode = false;
        if (__instance._hitUIInternal || !__instance._interactDown) 
            return;
        
        StickerSystem.Instance.PlaceStickerFromControllerRay(__instance.rayDirectionTransform, __instance.hand);
    }
}

internal static class ShaderFilterHelperPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShaderFilterHelper), nameof(ShaderFilterHelper.SetupFilter))]
    private static void Prefix_ShaderFilterHelper_SetupFilter()
    {
        if (!MetaPort.Instance.settings.GetSettingsBool("ExperimentalShaderLimitEnabled")) 
            return;
        
        StickerMod.Logger.Warning("ExperimentalShaderLimitEnabled found to be true. Disabling setting to prevent crashes when spawning stickers!");
        MetaPort.Instance.settings.SetSettingsBool("ExperimentalShaderLimitEnabled", false);
    }
}

internal static class CVRToolsPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRTools), nameof(CVRTools.ReplaceShaders), typeof(Material), typeof(string))]
    private static bool Prefix_CVRTools_ReplaceShaders(Material material, string fallbackShaderName = "")
    {
        if (material == null || material.shader == null) return true;
        return material.shader != StickerMod.DecalSimpleShader; // prevent replacing decals with fallback shader
    }
}