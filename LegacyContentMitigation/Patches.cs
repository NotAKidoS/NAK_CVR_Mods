using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.Base.Jobs;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.LocalClone;
using ABI_RC.Core.Player.TransformHider;
using ABI.CCK.Components;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NAK.LegacyContentMitigation.Patches;

internal static class PlayerSetup_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        FakeMultiPassHack.Instance = __instance.vrCam.AddComponentIfMissing<FakeMultiPassHack>();
        FakeMultiPassHack.Instance.enabled = ModSettings.EntryAutoForLegacyWorlds.Value;
    }
}

internal static class SceneLoaded_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SceneLoaded), nameof(SceneLoaded.OnSceneLoadedHandleJob))]
    private static void Prefix_SceneLoaded_OnSceneLoadedHandleJob()
    {
        if (!ModSettings.EntryAutoForLegacyWorlds.Value)
        {
            LegacyContentMitigationMod.Logger.Msg("LegacyContentMitigationMod is disabled.");
            FakeMultiPassHack.Instance.SetMultiPassActive(false);
            return;
        }
        
        bool sceneIsNotSpi = CVRWorld.CompatibilityVersion == CompatibilityVersions.NotSpi;
        string logText = sceneIsNotSpi
            ? "Legacy world detected, enabling legacy content mitigation."
            : "Loaded scene is not considered Legacy content. Disabling if active.";
        
        LegacyContentMitigationMod.Logger.Msg(logText);
        FakeMultiPassHack.Instance.SetMultiPassActive(sceneIsNotSpi);
    }
}

internal static class CVRWorld_Patches
{
    // Third Person patches same methods:
    // https://github.com/NotAKidoS/NAK_CVR_Mods/blob/3d6b1bbd59d23be19fe3594e104ad26e4ac0adcd/ThirdPerson/Patches.cs#L15-L22
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.CopyRefCamValues))]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.SetDefaultCamValues))]
    private static void Postfix_CVRWorld_SetDefaultCamValues(ref CVRWorld __instance)
    {
        LegacyContentMitigationMod.Logger.Msg("Legacy world camera values updated.");
        FakeMultiPassHack.Instance.OnMainCameraChanged();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.UpdatePostProcessing))]
    private static void Postfix_CVRWorld_UpdatePostProcessing(ref CVRWorld __instance)
    {
        if (!FakeMultiPassHack.Instance.IsActive) return;
        foreach (PostProcessEffectSettings motionBlur in __instance._postProcessingMotionBlurList)
            motionBlur.active = false; // force off cause its fucked and no one cares
    }
}

internal static class CVRTools_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRTools), nameof(CVRTools.ReplaceShaders), typeof(Material), typeof(string))]
    private static bool Prefix_CVRTools_ReplaceShaders(Material material, string fallbackShaderName = "")
    {
        // When in a legacy world with the hack enabled, do not replace shaders
        return !FakeMultiPassHack.Instance.IsActive;
    }
}

internal static class HeadHiderManager_Patches
{
    // despite the visual clone not being normally accessible, i fix it cause mod:
    // https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/VisualCloneFix
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderManager), nameof(TransformHiderManager.OnPreRenderCallback))]
    [HarmonyPatch(typeof(TransformHiderManager), nameof(TransformHiderManager.OnPreRenderCallback))]
    [HarmonyPatch(typeof(LocalCloneManager), nameof(LocalCloneManager.OnPreRenderCallback))]
    [HarmonyPatch(typeof(LocalCloneManager), nameof(LocalCloneManager.OnPostRenderCallback))]
    private static bool Prefix_HeadHiderManagers_OnRenderCallbacks(Camera cam)
    {
        if (!FakeMultiPassHack.Instance.IsActive) 
            return true; // not active, no need

        // dont let real camera trigger head hiding to occur or reset- leave it to the left/right eyes
        return !cam.CompareTag("MainCamera"); // we spoof playersetup.activeCam, need check tag
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderManager), nameof(TransformHiderManager.OnPostRenderCallback))]
    [HarmonyPatch(typeof(LocalCloneManager), nameof(LocalCloneManager.OnPostRenderCallback))]
    private static void Prefix_HeadHiderManagers_OnPostRenderCallback(Camera cam, ref MonoBehaviour __instance)
    {
        if (!FakeMultiPassHack.Instance.IsActive) return;

        if (FakeMultiPassHack.Instance.RenderingEye == Camera.MonoOrStereoscopicEye.Left)
            SetResetAfterRenderFlag(__instance, true); // so right eye mirror sees head

        if (FakeMultiPassHack.Instance.RenderingEye == Camera.MonoOrStereoscopicEye.Right)
            SetResetAfterRenderFlag(__instance, !TransformHiderManager.s_UseCloneToCullUi); // dont undo if ui culling

        return;
        void SetResetAfterRenderFlag(MonoBehaviour headHiderManager, bool flag)
        {
            if (headHiderManager is LocalCloneManager localCloneManager) 
                localCloneManager._resetAfterThisRender = flag;
            else if (headHiderManager is TransformHiderManager transformHiderManager) 
                transformHiderManager._resetAfterThisRender = flag;
        }
    }
}