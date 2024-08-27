using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.WhereAmIPointing;

public class WhereAmIPointingMod : MelonMod
{
    #region Melon Preferences
    
    // cannot disable because then id need extra logic to reset the alpha :)
    // private const string SettingsCategory = nameof(WhereAmIPointingMod);
    //
    // private static readonly MelonPreferences_Category Category =
    //     MelonPreferences.CreateCategory(SettingsCategory);
    //
    // private static readonly MelonPreferences_Entry<bool> Entry_Enabled =
    //     Category.CreateEntry("enabled", true, display_name: "Enabled",description: "Toggle WhereAmIPointingMod entirely.");
    
    #endregion Melon Preferences
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(ControllerRay_Patches));
    }
    
    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }

    #region Patches
    
     private static class ControllerRay_Patches
    {
        private const float ORIGINAL_ALPHA = 0.502f;
        private const float INTERACTION_ALPHA = 0.1f;
        private const float RAY_LENGTH = 1000f; // game normally raycasts to PositiveInfinity... -_-

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.LateUpdate))]
        private static void Postfix_ControllerRay_LateUpdate(ref ControllerRay __instance)
        {
            if (__instance.isDesktopRay 
                || !__instance.enabled 
                || !__instance.IsTracking() 
                || !__instance.lineRenderer)
                return;

            UpdateLineRendererAlpha(__instance);

            if (__instance.lineRenderer.enabled 
                || !ShouldOverrideLineRenderer(__instance))
                return;

            UpdateLineRendererPosition(__instance);
        }

        private static void UpdateLineRendererAlpha(ControllerRay instance)
        {
            Material material = instance.lineRenderer.material;
            Color color = material.color;

            float targetAlpha = instance.uiActive ? ORIGINAL_ALPHA : INTERACTION_ALPHA;
            if (!(Math.Abs(color.a - targetAlpha) > float.Epsilon)) 
                return;
            
            color.a = targetAlpha;
            material.color = color;
        }

        private static bool ShouldOverrideLineRenderer(ControllerRay instance)
        {
            if (!ViewManager.Instance.IsAnyMenuOpen)
                return false;

            if (CVR_MenuManager.Instance.IsQuickMenuOpen 
                && instance.hand == CVR_MenuManager.Instance.SelectedQuickMenuHand)
                return false;

            return true;
        }

        private static void UpdateLineRendererPosition(ControllerRay instance)
        {
            Vector3 rayOrigin = instance.rayDirectionTransform.position;
            Vector3 rayEnd = rayOrigin + instance.rayDirectionTransform.forward * RAY_LENGTH;

            instance.lineRenderer.SetPosition(0, instance.lineRenderer.transform.InverseTransformPoint(rayOrigin));
            instance.lineRenderer.SetPosition(1, instance.lineRenderer.transform.InverseTransformPoint(rayEnd));
            instance.lineRenderer.enabled = true;
        }
    }
    
    #endregion Patches
}