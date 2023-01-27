using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using HarmonyLib;
using UnityEngine;

namespace NAK.Melons.AASBufferFix.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "Start")]
    private static void Postfix_PuppetMaster_Start(ref PuppetMaster __instance)
    {
        AASBufferFix externalBuffer = __instance.AddComponentIfMissing<AASBufferFix>();
        externalBuffer.puppetMaster = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarInstantiated")]
    private static void Postfix_PuppetMaster_AvatarInstantiated(ref PuppetMaster __instance, ref Animator ____animator)
    {
        AASBufferFix externalBuffer = __instance.GetComponent<AASBufferFix>();
        if (externalBuffer != null) externalBuffer.OnAvatarInstantiated(____animator);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarDestroyed")]
    private static void Postfix_PuppetMaster_AvatarDestroyed(ref PuppetMaster __instance)
    {
        AASBufferFix externalBuffer = __instance.GetComponent<AASBufferFix>();
        if (externalBuffer != null) externalBuffer.OnAvatarDestroyed();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PuppetMaster), "ApplyAdvancedAvatarSettings")]
    private static bool Prefix_PuppetMaster_ApplyAdvancedAvatarSettings(float[] settingsFloat, int[] settingsInt, byte[] settingsByte, ref PuppetMaster __instance)
    {
        AASBufferFix externalBuffer = __instance.GetComponent<AASBufferFix>();
        if (externalBuffer != null && !externalBuffer.isAcceptingAAS)
        {
            externalBuffer.OnApplyAAS(settingsFloat, settingsInt, settingsByte);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRAnimatorManager), "ApplyAdvancedAvatarSettingsFromBuffer")]
    private static bool Prefix_PuppetMaster_ApplyAdvancedAvatarSettingsFromBuffer(ref Animator ____animator)
    {
        AASBufferFix externalBuffer = ____animator.GetComponentInParent<AASBufferFix>();
        if (externalBuffer != null && !externalBuffer.isAcceptingAAS)
        {
            //dont apply if stable buffer no exist
            return false;
        }
        return true;
    }
}