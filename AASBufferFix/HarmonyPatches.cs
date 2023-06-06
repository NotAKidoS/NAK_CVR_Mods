using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using HarmonyLib;
using UnityEngine;

namespace NAK.AASBufferFix.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "Start")]
    private static void Postfix_PuppetMaster_Start(ref PuppetMaster __instance)
    {
        __instance.AddComponentIfMissing<AASBufferHelper>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarInstantiated")]
    private static void Postfix_PuppetMaster_AvatarInstantiated(ref PuppetMaster __instance, ref Animator ____animator)
    {
        AASBufferHelper externalBuffer = __instance.GetComponent<AASBufferHelper>();

        externalBuffer?.OnAvatarInstantiated(____animator);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarDestroyed")]
    private static void Postfix_PuppetMaster_AvatarDestroyed(ref PuppetMaster __instance)
    {
        AASBufferHelper externalBuffer = __instance.GetComponent<AASBufferHelper>();

        externalBuffer?.OnAvatarDestroyed();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PuppetMaster), "ApplyAdvancedAvatarSettings")]
    private static bool Prefix_PuppetMaster_ApplyAdvancedAvatarSettings(float[] settingsFloat, int[] settingsInt, byte[] settingsByte, ref PuppetMaster __instance)
    {
        AASBufferHelper externalBuffer = __instance.GetComponent<AASBufferHelper>();
        if (externalBuffer != null && !externalBuffer.GameHandlesAAS)
        {
            externalBuffer.OnReceiveAAS(settingsFloat, settingsInt, settingsByte);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRAnimatorManager), "ApplyAdvancedAvatarSettingsFromBuffer")]
    private static bool Prefix_CVRAnimatorManager_ApplyAdvancedAvatarSettingsFromBuffer(ref Animator ____animator)
    {
        AASBufferHelper externalBuffer = ____animator.GetComponentInParent<AASBufferHelper>();
        if (externalBuffer != null && !externalBuffer.GameHandlesAAS)
        {
            //dont apply if stable buffer no exist
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), "SendAdvancedAvatarSettings")]
    private static bool Prefix_PlayerSetup_SendAdvancedAvatarSettings(ref PlayerSetup __instance)
    {
        //dont sync wrong settings to remote users
        return !__instance.avatarIsLoading;
    }
}