using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.TransformHider;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Camera;
using HarmonyLib;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderUtils), nameof(TransformHiderUtils.SetupAvatar))]
    private static bool OnSetupAvatar(GameObject avatar)
    {
        if (!AvatarCloneTestMod.EntryUseAvatarCloneTest.Value) return true;
        avatar.AddComponent<AvatarClone>();
        return false;
    }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(FPRExclusion), nameof(FPRExclusion.UpdateExclusions))]
    // private static void OnUpdateExclusions(ref FPRExclusion __instance)
    // {
    //     AvatarClone clone = PlayerSetup.Instance._avatar.GetComponent<AvatarClone>();
    //     if (clone == null) return;
    //     clone.SetBoneChainVisibility(__instance.target, !__instance.isShown, !__instance.shrinkToZero);
    // }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRMirror), nameof(CVRMirror.Start))]
    private static void OnMirrorStart(CVRMirror __instance)
    {
        if (!AvatarCloneTestMod.EntryUseAvatarCloneTest.Value)
            return;

        // Don't reflect the player clone layer
        __instance.m_ReflectLayers &= ~(1 << CVRLayers.PlayerClone);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Update))]
    private static void OnTransformHiderManagerUpdate(PlayerSetup __instance)
    {
        if (!AvatarCloneTestMod.EntryUseAvatarCloneTest.Value)
            return;
        
        if (MetaPort.Instance.settings.GetSettingsBool("ExperimentalAvatarOverrenderUI"))
            __instance.activeUiCam.cullingMask |= 1 << CVRLayers.PlayerClone;
        else
            __instance.activeUiCam.cullingMask &= ~(1 << CVRLayers.PlayerClone);
    }
    
    private static bool _wasDebugInPortableCamera;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.Update))]
    private static void OnPortableCameraUpdate(ref PortableCamera __instance)
    {
        if (!AvatarCloneTestMod.EntryUseAvatarCloneTest.Value)
        {
            // Show both PlayerLocal and PlayerClone
            __instance.cameraComponent.cullingMask |= 1 << CVRLayers.PlayerLocal;
            __instance.cameraComponent.cullingMask |= 1 << CVRLayers.PlayerClone;
            return;
        }

        if (TransformHiderManager.s_DebugInPortableCamera == _wasDebugInPortableCamera)
            return;
        
        if (TransformHiderManager.s_DebugInPortableCamera)
        {
            // Hide PlayerLocal, show PlayerClone
            __instance.cameraComponent.cullingMask &= ~(1 << CVRLayers.PlayerLocal);
            __instance.cameraComponent.cullingMask |= 1 << CVRLayers.PlayerClone;
        }
        else
        {
            // Show PlayerLocal, hide PlayerClone
            __instance.cameraComponent.cullingMask |= 1 << CVRLayers.PlayerLocal;
            __instance.cameraComponent.cullingMask &= ~(1 << CVRLayers.PlayerClone);
        }
        
        _wasDebugInPortableCamera = TransformHiderManager.s_DebugInPortableCamera;
    }
}