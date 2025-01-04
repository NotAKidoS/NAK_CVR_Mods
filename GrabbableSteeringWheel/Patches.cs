using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using UnityEngine;

namespace NAK.GrabbableSteeringWheel.Patches;

internal static class RCCCarControllerV3_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RCC_CarControllerV3), nameof(RCC_CarControllerV3.Awake))]
    private static void Postfix_RCC_CarControllerV3_Awake(ref RCC_CarControllerV3 __instance)
    {
        Transform steeringWheelTransform = __instance.SteeringWheel;
        if (steeringWheelTransform == null) 
            return;

        RCC_CarControllerV3 v3 = __instance;
        BoneVertexBoundsUtility.CalculateBoneWeightedBounds(
            steeringWheelTransform, 
            0.8f, 
            BoneVertexBoundsUtility.BoundsCalculationFlags.All,
            result =>
        {
            if (!result.IsValid)
                return;
            
            BoxCollider boxCollider = steeringWheelTransform.gameObject.AddComponent<BoxCollider>();
            boxCollider.center = result.LocalBounds.center;
            boxCollider.size = result.LocalBounds.size;
            
            SteeringWheelPickup steeringWheel = steeringWheelTransform.gameObject.AddComponent<SteeringWheelPickup>();
            steeringWheel.SetupSteeringWheel(v3);
        });
    }
}

internal static class CVRInputManager_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), nameof(CVRInputManager.UpdateInput))]
    private static void Postfix_CVRInputManager_UpdateInput(ref CVRInputManager __instance)
    {
        // Steering input is clamped in RCC component
        if (BetterBetterCharacterController.Instance.IsSittingOnControlSeat())
            __instance.steering += SteeringWheelPickup.GetSteerInput(
                BetterBetterCharacterController.Instance._lastCvrSeat._carController);
    }
}