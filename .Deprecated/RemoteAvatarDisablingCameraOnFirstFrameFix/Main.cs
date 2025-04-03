using System.Reflection;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.RemoteAvatarDisablingCameraOnFirstFrameFix;

public class RemoteAvatarDisablingCameraOnFirstFrameFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PuppetMaster).GetMethod(nameof(PuppetMaster.AvatarInstantiated),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(RemoteAvatarDisablingCameraOnFirstFrameFixMod).GetMethod(nameof(OnPuppetMasterAvatarInstantiated),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnPuppetMasterAvatarInstantiated(PuppetMaster __instance)
    {
        if (__instance._animator == null) return;
        
        __instance._animator.WriteDefaultValues();
        __instance._animator.keepAnimatorStateOnDisable = false;
        __instance._animator.writeDefaultValuesOnDisable = false;
    }

    // private static void OnPuppetMasterAvatarInstantiated(PuppetMaster __instance)
    // {
    //     if (__instance._animator == null) return;
    //     
    //     // Set culling mode to always animate
    //     __instance._animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    //     
    //     // Update the animator to force it to do the first frame
    //     __instance._animator.Update(0f);
    //     
    //     // Set culling mode back to cull update transforms
    //     __instance._animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    // }
}