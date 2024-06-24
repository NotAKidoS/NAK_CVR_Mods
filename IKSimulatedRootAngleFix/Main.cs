using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.VRIKHandlers;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.IKSimulatedRootAngleFix;

public class IKSimulatedRootAngleFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // fix offsetting of _ikSimulatedRootAngle when player rotates on wall or ceiling
            typeof(IKHandler).GetMethod(nameof(IKHandler.OnPlayerHandleMovementParent),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(IKSimulatedRootAngleFixMod).GetMethod(nameof(OnPlayerHandleMovementParent),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // why did i dupe logic weirdly between Desktop & VR IKHandler ...
            typeof(IKHandlerDesktop).GetMethod(nameof(IKHandlerDesktop.HandleBodyHeading),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(IKSimulatedRootAngleFixMod).GetMethod(nameof(OnHandleBodyHeading),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch( // why did i dupe logic weirdly between Desktop & VR IKHandler ...
            typeof(IKHandlerHalfBody).GetMethod(nameof(IKHandlerHalfBody.HandleRootAngle),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(IKSimulatedRootAngleFixMod).GetMethod(nameof(OnHandleRootAngle),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static float GetRemappedPlayerHeading()
    {
        // invert, remap, then remap again so it matches playerlocal.eulerAngles.y that originally was used
        // NOTE: we want to still use GetPlayerHeading because it accounts for gravity (and cause exterrata helped with that method specifically :3)
        // NOTE: i am remapping it to match *original* DesktopVRIK logic before i reworked it native- dropping this remap into native method would not work
        var playerHeading = (-IKSystem.Instance.GetPlayerHeading() + 180) % 360;
        if (playerHeading < 0) playerHeading += 360;
        return playerHeading;
    }
    
    private static bool OnPlayerHandleMovementParent(BetterBetterCharacterController.PlayerMoveOffset moveOffset, ref IKHandler __instance)
    {
        __instance._solver.AddPlatformMotion(moveOffset.DeltaPosition, moveOffset.DeltaRotation, moveOffset.PivotPosition);
    
        Transform playerTransform = IKSystem.Instance.transform;
        Vector3 up = playerTransform.up; // for simplicity, we will just use the current up, instead of calc past up
        
        Quaternion playerRotation = playerTransform.rotation;
        Quaternion deltaRotation = moveOffset.DeltaRotation;

        // calculate the player's forward direction before the delta rotation was applied
        Quaternion originalRotation = Quaternion.Inverse(deltaRotation) * playerRotation;

        Vector3 forwardBeforeRotation = Vector3.ProjectOnPlane(originalRotation * Vector3.forward, up).normalized;
        Vector3 forwardAfterRotation = Vector3.ProjectOnPlane(playerRotation * Vector3.forward, up).normalized;

        // calculate the signed angle between the forward directions before and after the rotation around the up vector
        float headingDelta = Vector3.SignedAngle(forwardBeforeRotation, forwardAfterRotation, up);
        __instance._ikSimulatedRootAngle = Mathf.Repeat(__instance._ikSimulatedRootAngle + headingDelta, 360f);
        return false;
    }
    
    private static bool OnHandleBodyHeading(ref IKHandlerDesktop __instance)
    {
        if (IKSystem.Instance.BodyHeadingLimit <= 0f)
            return false;
        
        float playerHeading = GetRemappedPlayerHeading();
        
        // nicked original logic from DesktopVRIK, before i made it native and seemingly fucked it -_-
        // https://github.com/NotAKidOnSteam/NAK_CVR_Mods/blob/db9d5a24b62c96e3c5c403ce3956cd3221955898/.DepricatedMods/DesktopVRIK/IK/IKHandlers/IKHandlerDesktop.cs#L68
        var weightedAngleLimit = IKSystem.Instance.BodyHeadingLimit * __instance._solver.locomotion.weight;
        var deltaAngleRoot = Mathf.DeltaAngle(playerHeading, __instance._ikSimulatedRootAngle);
        var absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);
        if (absDeltaAngleRoot > weightedAngleLimit)
        {
            deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
            __instance._ikSimulatedRootAngle = Mathf.MoveTowardsAngle(__instance._ikSimulatedRootAngle,
                playerHeading, absDeltaAngleRoot - weightedAngleLimit);
        }

        __instance._solver.spine.rootHeadingOffset = deltaAngleRoot;

        Vector3 axis = __instance._vrik.transform.rotation * Vector3.up;
        if (IKSystem.Instance.PelvisHeadingWeight > 0f)
        {
            __instance._solver.spine.pelvisRotationOffset *= Quaternion.AngleAxis(
                __instance._solver.spine.rootHeadingOffset * IKSystem.Instance.PelvisHeadingWeight, axis);
            __instance._solver.spine.chestRotationOffset *= Quaternion.AngleAxis(
                -__instance._solver.spine.rootHeadingOffset * IKSystem.Instance.PelvisHeadingWeight, axis);
        }

        if (IKSystem.Instance.ChestHeadingWeight > 0f)
            __instance._solver.spine.chestRotationOffset *= Quaternion.AngleAxis(
                __instance._solver.spine.rootHeadingOffset * IKSystem.Instance.ChestHeadingWeight, axis);

        return false;
    }

    private static bool OnHandleRootAngle(ref IKHandlerHalfBody __instance)
    {
        float playerHeading = GetRemappedPlayerHeading();

        var rootAngleLimit = 25f;
        if (__instance._solver.spine.rotationWeight <= 0f || PlayerSetup.Instance.IsEmotePlaying())
            rootAngleLimit = 180f;
        else if (CVRInputManager.Instance.movementVector.sqrMagnitude > 0f) rootAngleLimit = 0f;

        var weightedAngleLimit = rootAngleLimit * __instance._solver.locomotion.weight;
        var deltaAngleRoot = Mathf.DeltaAngle(playerHeading, __instance._ikSimulatedRootAngle);
        var absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);
        if (absDeltaAngleRoot > weightedAngleLimit)
        {
            deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
            __instance._ikSimulatedRootAngle = Mathf.MoveTowardsAngle(__instance._ikSimulatedRootAngle,
                playerHeading, absDeltaAngleRoot - weightedAngleLimit);
        }

        __instance._solver.spine.maxRootAngle = rootAngleLimit;
        __instance._solver.spine.rootHeadingOffset = deltaAngleRoot;
        return false;
    }
}