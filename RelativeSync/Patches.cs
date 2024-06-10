using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.Jobs;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using HarmonyLib;
using NAK.RelativeSync.Components;
using NAK.RelativeSync.Networking;
using UnityEngine;

namespace NAK.RelativeSync.Patches;

internal static class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMonitor>();
    }
}

internal static class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.Start))]
    private static void Postfix_PuppetMaster_Start(ref PuppetMaster __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncController>();
    }
}

internal static class CVRSeatPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSeat), nameof(CVRSeat.Awake))]
    private static void Postfix_CVRSeat_Awake(ref CVRSeat __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMarker>();
    }
    
    internal static bool canUpdate;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRSeat), nameof(CVRSeat.Update))]
    private static bool Prefix_CVRSpawnable_Update()
        => !BetterBetterCharacterControllerPatches.UseSeatAndPickupsHack || canUpdate;
}

internal static class CVRMovementParentPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRMovementParent), nameof(CVRMovementParent.Start))]
    private static void Postfix_CVRMovementParent_Start(ref CVRMovementParent __instance)
    {
        __instance.AddComponentIfMissing<RelativeSyncMarker>();
    }
}

internal static class NetworkRootDataUpdatePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkRootDataUpdate), nameof(NetworkRootDataUpdate.Submit))]
    private static void Postfix_NetworkRootDataUpdater_Submit()
    {
        ModNetwork.SendRelativeSyncUpdate(); // Send the relative sync update after the network root data update
    }
}

internal static class CVRSpawnablePatches
{
    internal static bool UseHack;
    
    private static bool _canUpdate;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.Update))]
    private static bool Prefix_CVRSpawnable_Update()
        => !UseHack || _canUpdate;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.FixedUpdate))]
    private static void Postfix_CVRSpawnable_FixedUpdate(ref CVRSpawnable __instance)
    {
        if (!UseHack) return;
        
        _canUpdate = true;
        __instance.Update();
        _canUpdate = false;
    }
}

internal static class BetterBetterCharacterControllerPatches
{
    private static bool _noInterpolation;
    internal static bool NoInterpolation
    {
        get => _noInterpolation;
        set
        {
            _noInterpolation = value;
            if (_rigidbody == null) return;
            _rigidbody.interpolation = value ? RigidbodyInterpolation.None : _initialInterpolation;
        }
    }
    
    private static Rigidbody _rigidbody;
    private static RigidbodyInterpolation _initialInterpolation;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BetterBetterCharacterController), nameof(BetterBetterCharacterController.Start))]
    private static void Postfix_BetterBetterCharacterController_Update(ref BetterBetterCharacterController __instance)
    {
        _rigidbody = __instance.GetComponent<Rigidbody>();
        _initialInterpolation = _rigidbody.interpolation;
        NoInterpolation = _noInterpolation; // get initial value as patch runs later than settings init
    }
    
    internal static bool UseSeatAndPickupsHack;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BetterBetterCharacterController), nameof(BetterBetterCharacterController.OnAfterSimulationUpdate))]
    private static void Postfix_BetterBetterCharacterController_OnAfterSimulationUpdate(ref CVRSeat ____lastCvrSeat)
    {
        if (!UseSeatAndPickupsHack) 
            return;

        // solve chairs
        if (____lastCvrSeat != null)
        {
            CVRSeatPatches.canUpdate = true;
            ____lastCvrSeat.Update();
            CVRSeatPatches.canUpdate = false;    
        } 
        
        // now solve held pickups (very hacky)
        CVRPickupObjectPatches.canUpdate = true;
        
        Pickupable heldPickup;
        if (!MetaPort.Instance.isUsingVr)
        {
            heldPickup = PlayerSetup.Instance.desktopRay.grabbedObject;
            if (heldPickup != null && heldPickup is CVRPickupObject pickupObject)
                CVRPickupObjectPatches.FixedFixedUpdate(pickupObject);
        }
        else
        {
            heldPickup = PlayerSetup.Instance.vrRayLeft.grabbedObject;
            if (heldPickup != null && heldPickup is CVRPickupObject pickupObjectLeft)
                CVRPickupObjectPatches.FixedFixedUpdate(pickupObjectLeft);
            heldPickup = PlayerSetup.Instance.vrRayRight.grabbedObject;
            if (heldPickup != null && heldPickup is CVRPickupObject pickupObjectRight)
                CVRPickupObjectPatches.FixedFixedUpdate(pickupObjectRight);
        }
        
        CVRPickupObjectPatches.canUpdate = false;
    }
}

internal static class CVRPickupObjectPatches
{
    internal static bool canUpdate;
    internal static float themultiplier = 20f;

    // if (!(pickupObject.transform.position.y >= pickupObject._respawnHeight))
    //      pickupObject.ResetLocation(); // reset if below respawn height
    
    internal static void FixedBasicUpdate(CVRPickupObject pickupObject)
    {
        if (pickupObject._currentlyFlung && pickupObject._directFlung)
        {
            pickupObject.isTeleGrabbed = true;
            pickupObject.transform.position = 
                Vector3.SmoothDamp(pickupObject.transform.position, pickupObject._flingTarget, ref pickupObject._flingVelocity, 0.25f);
            pickupObject._rigidbody.useGravity = false;
            if (Vector3.Distance(pickupObject.transform.position, pickupObject._flingTarget) < 0.1f)
            {
                pickupObject.ResetFlungStatus();
                SchedulerSystem.RemoveJob(pickupObject.ResetFlungStatus);
            }
        }
        
        if (pickupObject.updateWithPhysics
            || pickupObject.ControllerRay == null) 
            return;

		if (pickupObject.gripType == CVRPickupObject.GripType.Free)
			pickupObject.transform.rotation = pickupObject.ControllerRay.pivotPoint.rotation;
		
		else if (pickupObject.gripType == CVRPickupObject.GripType.Origin)
		{
			if (pickupObject.gripOrigin == null || pickupObject.gripOrigin == pickupObject.transform)
			{
				pickupObject.transform.rotation = pickupObject.ControllerRay.pivotPoint.rotation;
			}
			else
			{
				pickupObject.transform.rotation = 
                    Quaternion.Inverse(pickupObject.gripOrigin.localRotation * Quaternion.Inverse(pickupObject.ControllerRay.pivotPoint.rotation));
				Vector3 b = pickupObject.ControllerRay.pivotPoint.position - pickupObject.gripOrigin.position;
				pickupObject.transform.position += b;
			}
		}

		Vector3 vector = pickupObject.transform.position;
		if (pickupObject.gripType == CVRPickupObject.GripType.Free)
		{
			vector = pickupObject.ControllerRay.pivotPoint.position + pickupObject.ControllerRay.pivotPoint.TransformDirection(pickupObject._initialPositionOffset) - pickupObject.transform.position;
		}
		if (pickupObject.gripType == CVRPickupObject.GripType.Origin)
		{
			if (pickupObject.gripOrigin == null)
			{
				vector = pickupObject.ControllerRay.pivotPoint.position - pickupObject.transform.position;
			}
			else
			{
				vector = pickupObject.ControllerRay.pivotPoint.position - pickupObject.gripOrigin.position;
			}
		}

        var snappingVelocity = pickupObject.GetSnappingVelocity(
            out Vector3 snappingPointPosition, 
            out CVRSnappingPoint snappingPoint, 
            out SnappingReference snappingReference);
            
        if (snappingVelocity.HasValue 
            && snappingPoint != null 
            && snappingReference != null &&
            Vector3.Distance(
                pickupObject.transform.position + vector + snappingReference.referencePoint.position -
                pickupObject.transform.position, snappingPointPosition) < snappingPoint.distance * 1.25)
            vector = snappingVelocity.Value;
        
		pickupObject.transform.position += vector;
		pickupObject._resultVelocity = vector / Time.deltaTime;
    }
    
    internal static void FixedFixedUpdate(CVRPickupObject pickupObject)
    {
        if (!pickupObject.updateWithPhysics || pickupObject.ControllerRay == null)
            return;

        // Determine the target position and rotation based on the grip type
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = pickupObject.ControllerRay.pivotPoint.rotation;

        if (pickupObject.gripType == CVRPickupObject.GripType.Free)
        {
            targetPosition = pickupObject.ControllerRay.pivotPoint.position +
                             pickupObject.ControllerRay.pivotPoint.TransformDirection(pickupObject._initialPositionOffset);
        }
        else if (pickupObject.gripType == CVRPickupObject.GripType.Origin)
        {
            targetPosition = pickupObject.gripOrigin != null
                ? pickupObject.ControllerRay.pivotPoint.position - pickupObject.gripOrigin.position + pickupObject.transform.position
                : pickupObject.ControllerRay.pivotPoint.position;
        }

        // Handle snapping if necessary
        var snappingVelocity = pickupObject.GetSnappingVelocity(
            out Vector3 snappingPointPosition,
            out CVRSnappingPoint snappingPoint,
            out SnappingReference snappingReference);

        if (snappingVelocity.HasValue &&
            snappingPoint != null &&
            snappingReference != null &&
            Vector3.Distance(targetPosition + snappingReference.referencePoint.position - pickupObject.transform.position, snappingPointPosition) < snappingPoint.distance * 1.25)
        {
            targetPosition = snappingVelocity.Value + pickupObject.transform.position;
        }

        // Smoothly interpolate to the target position and rotation
        // pickupObject._rigidbody.position = Vector3.Lerp(pickupObject._rigidbody.position, targetPosition, Time.fixedDeltaTime * 10f);
        // pickupObject._rigidbody.rotation = Quaternion.Slerp(pickupObject._rigidbody.rotation, targetRotation, Time.fixedDeltaTime * 10f);

        // Apply small corrective forces to handle collisions more naturally
        Vector3 correctiveForce = (targetPosition - pickupObject._rigidbody.position) / Time.fixedDeltaTime;
        pickupObject._rigidbody.AddForce(correctiveForce, ForceMode.VelocityChange);
        //pickupObject._rigidbody.AddTorque(Vector3.Cross(pickupObject._rigidbody.angularVelocity, correctiveForce) * themultiplier, ForceMode.VelocityChange);
        
        // Reset angular velocity to ensure precise control over rotation
        //pickupObject._rigidbody.angularVelocity = Vector3.zero;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.FixedUpdate))]
    private static bool Prefix_CVRPickupObject_FixedUpdate(ref CVRPickupObject __instance)
    {
        if (!BetterBetterCharacterControllerPatches.UseSeatAndPickupsHack) 
            return true;

        if (!(__instance.transform.position.y >= __instance._respawnHeight))
            __instance.ResetLocation(); // reset if below respawn height

        return canUpdate;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Update))]
    private static bool Prefix_CVRPickupObject_Update(ref CVRPickupObject __instance)
    {
        if (!BetterBetterCharacterControllerPatches.UseSeatAndPickupsHack) 
            return true;
        
        return canUpdate;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Start))]
    private static void Postfix_CVRPickupObject_Start(ref CVRPickupObject __instance)
    {
        __instance.AddComponentIfMissing<FixedJointPickupable>();
        UnityEngine.Object.Destroy(__instance);
    }
}