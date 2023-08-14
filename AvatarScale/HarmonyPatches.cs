using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using ABI_RC.Systems.IK;
using RootMotion.FinalIK;

namespace NAK.AvatarScaleMod.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    private static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance)
    {
        try
        {
            __instance._avatar.AddComponent<AvatarScaleManager>().Initialize(__instance._initialAvatarHeight, __instance.initialScale, true);

        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatar)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}

internal class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.AvatarInstantiated))]
    private static void Postfix_PuppetMaster_AvatarInstantiated(ref PuppetMaster __instance)
    {
        try
        {
            __instance.avatarObject.AddComponent<AvatarScaleManager>().Initialize(__instance._initialAvatarHeight, __instance.initialAvatarScale, false);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PuppetMaster_AvatarInstantiated)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}

internal class GesturePlaneTestPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GesturePlaneTest), nameof(GesturePlaneTest.Start))]
    private static void Postfix_GesturePlaneTest_Start()
    {
        try
        {
            // nicked from Kafe >:))))

            // This requires arms far outward- pull inward with fist and triggers.
            // Release triggers while still holding fist to readjust.

            var gesture = new CVRGesture
            {
                name = "avatarScaleIn",
                type = CVRGesture.GestureType.Hold,
            };
            gesture.steps.Add(new CVRGestureStep
            {
                firstGesture = CVRGestureStep.Gesture.Fist,
                secondGesture = CVRGestureStep.Gesture.Fist,
                startDistance = 1f,
                endDistance = 0.25f,
                direction = CVRGestureStep.GestureDirection.MovingIn,
            });
            gesture.onStart.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleStart));
            gesture.onStay.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleStay));
            gesture.onEnd.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleEnd));
            CVRGestureRecognizer.Instance.gestures.Add(gesture);

            gesture = new CVRGesture
            {
                name = "avatarScaleOut",
                type = CVRGesture.GestureType.Hold,
            };
            gesture.steps.Add(new CVRGestureStep
            {
                firstGesture = CVRGestureStep.Gesture.Fist,
                secondGesture = CVRGestureStep.Gesture.Fist,
                startDistance = 0.25f,
                endDistance = 1f,
                direction = CVRGestureStep.GestureDirection.MovingOut,
            });
            gesture.onStart.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleStart));
            gesture.onStay.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleStay));
            gesture.onEnd.AddListener(new UnityAction<float, Transform, Transform>(AvatarScaleGesture.OnScaleEnd));
            CVRGestureRecognizer.Instance.gestures.Add(gesture);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_GesturePlaneTest_Start)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}