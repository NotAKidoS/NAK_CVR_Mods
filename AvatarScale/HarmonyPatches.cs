using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.AvatarScaleMod.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance)
    {
        try
        {
            __instance._avatar.AddComponent<AvatarScaleManager>().Initialize(__instance._initialAvatarHeight, __instance.initialScale);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatar)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}

class GesturePlaneTestPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GesturePlaneTest), nameof(GesturePlaneTest.Start))]
    static void Postfix_GesturePlaneTest_Start()
    {
        try
        {
            // nicked from Kafe >:))))
            var gesture = new CVRGesture
            {
                name = "avatarScale",
                type = CVRGesture.GestureType.Hold,
            };
            gesture.steps.Add(new CVRGestureStep
            {
                firstGesture = CVRGestureStep.Gesture.Fist,
                secondGesture = CVRGestureStep.Gesture.Fist,
                startDistance = 0.5f,
                endDistance = 0.4f,
                direction = CVRGestureStep.GestureDirection.MovingIn,
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