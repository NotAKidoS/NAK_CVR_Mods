using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using NAK.RCCVirtualSteeringWheel.Util;
using UnityEngine;

namespace NAK.RCCVirtualSteeringWheel.Patches;

internal static class RCCCarControllerV3_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RCC_CarControllerV3), nameof(RCC_CarControllerV3.Awake))]
    private static void Postfix_RCC_CarControllerV3_Awake(RCC_CarControllerV3 __instance)
    {
        Transform steeringWheelTransform = __instance.SteeringWheel;
        if (steeringWheelTransform == null) 
            return;

        BoneVertexBoundsUtility.CalculateBoneWeightedBounds(
            steeringWheelTransform, 
            0.8f, 
            BoneVertexBoundsUtility.BoundsCalculationFlags.All,
            result =>
        {
            if (!result.IsValid)
                return;

            if (!__instance)
                return;
            
            SteeringWheelRoot.SetupSteeringWheel(__instance, result.LocalBounds);
        });
    }
}

internal static class CVRInputManager_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), nameof(CVRInputManager.UpdateInput))]
    private static void Postfix_CVRInputManager_UpdateInput(ref CVRInputManager __instance)
    {
        BetterBetterCharacterController characterController = BetterBetterCharacterController.Instance;
        if (!characterController._isSitting)
            return; // Must be sitting
        
        CVRSeat cvrSeat = characterController._lastCvrSeat;
        if (!cvrSeat
            || !cvrSeat.lockControls)
            return; // Must be a driver seat

        RCC_CarControllerV3 carController = characterController._lastCvrSeat._carController;
        if (!carController)
            return; // Specific to RCC
          
        if (SteeringWheelRoot.TryGetWheelInput(carController, out float steeringValue))
            __instance.steering = steeringValue;
    }
}