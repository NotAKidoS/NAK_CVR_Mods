using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NAK.ViewpointHeadScaleFix;

// Makes initialCameraPos scale with head bone scale

public class ViewpointHeadScaleFix : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.CalibrateAvatar)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(ViewpointHeadScaleFix).GetMethod(nameof(OnPlayerSetupCalibrateAvatar_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SetViewPointOffset)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(ViewpointHeadScaleFix).GetMethod(nameof(OnPlayerSetupSetViewPointOffset_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnPlayerSetupCalibrateAvatar_Postfix(ref PlayerSetup __instance)
    {
        if (!MetaPort.Instance.isUsingVr) 
            return;

        Transform headTransform = __instance._animator.GetBoneTransform(HumanBodyBones.Head);
        if (headTransform == null) 
            return;
        
        _originalCameraPos = __instance.initialCameraPos;
        _initialHeadScale = headTransform.localScale;
    }

    private static void OnPlayerSetupSetViewPointOffset_Prefix(ref PlayerSetup __instance)
    {
        if (!MetaPort.Instance.isUsingVr)
            return;
        
        Transform headTransform = __instance._animator.GetBoneTransform(HumanBodyBones.Head);
        if (headTransform == null) 
            return;
        
        __instance.initialCameraPos = __instance.MultiplyVectors(
            _originalCameraPos,
            __instance.DivideVectors(headTransform.localScale, _initialHeadScale)
        );
    }

    private static Vector3 _originalCameraPos = Vector3.one;
    private static Vector3 _initialHeadScale = Vector3.one;
}