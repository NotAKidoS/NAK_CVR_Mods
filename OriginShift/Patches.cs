#if !UNITY_EDITOR
using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.Camera;
using ABI_RC.Systems.Communications.Networking;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using DarkRift;
using HarmonyLib;
using NAK.OriginShift.Components;
using NAK.OriginShift.Hacks;
using UnityEngine;
using Zettai;

namespace NAK.OriginShift.Patches;

internal static class BetterBetterCharacterControllerPatches
{
    internal static bool PreventNextClearMovementParent;
    internal static bool PreventNextClearAccumulatedForces;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BetterBetterCharacterController), nameof(BetterBetterCharacterController.Start))]
    private static void Postfix_BetterCharacterController_Start(ref BetterBetterCharacterController __instance)
    {
        __instance.AddComponentIfMissing<OriginShiftMonitor>();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BetterBetterCharacterController), nameof(BetterBetterCharacterController.ClearLastMovementParent))]
    private static bool Prefix_BetterCharacterController_ClearLastMovementParent()
    {
        if (!PreventNextClearMovementParent) 
            return true;
        
        // skip this call if we are preventing it
        PreventNextClearMovementParent = false;
        Debug.Log("Prevented ClearLastMovementParent");
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BetterBetterCharacterController), nameof(BetterBetterCharacterController.ClearAccumulatedForces))]
    private static bool Prefix_BetterCharacterController_ClearAccumulatedForces()
    {
        if (!PreventNextClearAccumulatedForces) 
            return true;
        
        // skip this call if we are preventing it
        PreventNextClearAccumulatedForces = false;
        Debug.Log("Prevented ClearAccumulatedForces");
        return false;
    }
}

internal static class CVRSpawnablePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.Start))]
    private static void Postfix_CVRSpawnable_Start(ref CVRSpawnable __instance)
    {
        Transform wrapper = __instance.transform.parent;
        
        // test adding to the wrapper of the spawnable
        wrapper.AddComponentIfMissing<OriginShiftTransformReceiver>();
        wrapper.AddComponentIfMissing<OriginShiftParticleSystemReceiver>();
        wrapper.AddComponentIfMissing<OriginShiftTrailRendererReceiver>();
        wrapper.AddComponentIfMissing<OriginShiftRigidbodyReceiver>();
    }
    
    [HarmonyPrefix] // inbound spawnable
    [HarmonyPatch(typeof(CVRSpawnable), nameof(CVRSpawnable.UpdateFromNetwork))]
    private static void Prefix_CVRSpawnable_UpdateFromNetwork(ref CVRSyncHelper.PropData propData)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to absolute world position
        {
            Vector3 position = new(propData.PositionX, propData.PositionY, propData.PositionZ); // imagine not using Vector3
            position = OriginShiftManager.GetLocalizedPosition(position);
            propData.PositionX = position.x;
            propData.PositionY = position.y;
            propData.PositionZ = position.z;
        }
    }
}

internal static class RCC_SkidmarksManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RCC_SkidmarksManager), nameof(RCC_SkidmarksManager.Start))]
    private static void Postfix_RCC_SkidMarkManager_Start(ref RCC_SkidmarksManager __instance)
    {
        __instance.gameObject.AddComponentIfMissing<OriginShiftTransformReceiver>();
    }
}

internal static class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.desktopCam.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
        __instance.vrCam.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.GetPlayerMovementData))]
    private static void Postfix_PlayerSetup_GetPlayerMovementData(ref PlayerAvatarMovementData __result)
    {
        if (OriginShiftManager.CompatibilityMode)
        {
            // adjust root position back to absolute world position
            __result.RootPosition = OriginShiftManager.GetAbsolutePosition(__result.RootPosition); // player root
            __result.BodyPosition = OriginShiftManager.GetAbsolutePosition(__result.BodyPosition); // player hips (pls fix, why in world space?)
        } 
    }
}

internal static class PortableCameraPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.Start))]
    private static void Postfix_PortableCamera_Start(ref PortableCamera __instance)
    {
        __instance.cameraComponent.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
    }
}

internal static class PathingCameraPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRPathCamController), nameof(CVRPathCamController.Start))]
    private static void Postfix_CVRPathCamController_Start(ref CVRPathCamController __instance)
    {
        __instance.cam.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
    }
}

internal static class Comms_ClientPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Comms_Client), nameof(Comms_Client.SetPosition))]
    private static void Prefix_Comms_Client_GetPlayerMovementData(ref Vector3 listenerPosition)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to absolute world position
            listenerPosition = OriginShiftManager.GetAbsolutePosition(listenerPosition);
    }
}

internal static class CVRSyncHelperPatches
{
    [HarmonyPrefix] // outbound spawnable
    [HarmonyPatch(typeof(CVRSyncHelper), nameof(CVRSyncHelper.UpdatePropValues))]
    private static void Prefix_CVRSyncHelper_UpdatePropValues(ref Vector3 position)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to absolute world position
            position = OriginShiftManager.GetAbsolutePosition(position);
    }

    [HarmonyPrefix] // outbound object sync
    [HarmonyPatch(typeof(CVRSyncHelper), nameof(CVRSyncHelper.MoveObject))]
    private static void Prefix_CVRSyncHelper_MoveObject(ref float PosX, ref float PosY, ref float PosZ)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to absolute world position
        {
            Vector3 position = new(PosX, PosY, PosZ); // imagine not using Vector3
            position = OriginShiftManager.GetAbsolutePosition(position);
            PosX = position.x;
            PosY = position.y;
            PosZ = position.z;
        }
    }

    [HarmonyPrefix] // outbound spawn prop
    [HarmonyPatch(typeof(CVRSyncHelper), nameof(CVRSyncHelper.SpawnProp))]
    private static void Prefix_CVRSyncHelper_SpawnProp(ref float posX, ref float posY, ref float posZ)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to absolute world position
        {
            Vector3 position = new(posX, posY, posZ); // imagine not using Vector3
            position = OriginShiftManager.GetAbsolutePosition(position);
            posX = position.x;
            posY = position.y;
            posZ = position.z;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSyncHelper), nameof(CVRSyncHelper.SpawnPropFromNetwork))]
    private static void Postfix_CVRSyncHelper_SpawnPropFromNetwork(Message message)
    {
        if (!OriginShiftManager.CompatibilityMode)
            return;
        
        using DarkRiftReader reader = message.GetReader();
        reader.ReadString(); // objectId, don't care
        
        string instanceId = reader.ReadString();
        CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find((match) => match.InstanceId == instanceId);
        if (propData == null)
            return; // uh oh

        Vector3 position = new(propData.PositionX, propData.PositionY, propData.PositionZ); // imagine not using Vector3
        position = OriginShiftManager.GetLocalizedPosition(position);
        propData.PositionX = position.x;
        propData.PositionY = position.y;
        propData.PositionZ = position.z;
    }
}

internal static class CVRObjectSyncPatches
{
    [HarmonyPrefix] // inbound object sync
    [HarmonyPatch(typeof(CVRObjectSync), nameof(CVRObjectSync.receiveNetworkData))]
    [HarmonyPatch(typeof(CVRObjectSync), nameof(CVRObjectSync.receiveNetworkDataJoin))]
    private static void Prefix_CVRObjectSync_Update(ref Vector3 position)
    {
        if (OriginShiftManager.CompatibilityMode) // adjust root position back to localized world position
            position = OriginShiftManager.GetLocalizedPosition(position);
    }
}

internal static class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.Start))]
    private static void Postfix_PuppetMaster_Start(ref PuppetMaster __instance)
    {
        __instance.gameObject.AddComponentIfMissing<OriginShiftNetIkReceiver>();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.CycleData))]
    private static void Prefix_PuppetMaster_CycleData(ref PlayerAvatarMovementData ___PlayerAvatarMovementDataInput)
    {
        if (OriginShiftManager.CompatibilityMode) // && if user is not using OriginShift
        {
            // adjust root position back to absolute world position
            ___PlayerAvatarMovementDataInput.RootPosition = OriginShiftManager.GetLocalizedPosition(___PlayerAvatarMovementDataInput.RootPosition); // player root
            ___PlayerAvatarMovementDataInput.BodyPosition = OriginShiftManager.GetLocalizedPosition(___PlayerAvatarMovementDataInput.BodyPosition); // player hips (pls fix, why in world space?)
        }
    }
}

//OriginShiftDbAvatarReceiver
internal static class DbJobsAvatarManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DbJobsAvatarManager), nameof(DbJobsAvatarManager.Awake))]
    private static void Postfix_DbJobsAvatarManager_Start(ref DbJobsAvatarManager __instance)
    {
        __instance.gameObject.AddComponentIfMissing<OriginShiftDbAvatarReceiver>();
    }
}

// CVRPortalManager
internal static class CVRPortalManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRPortalManager), nameof(CVRPortalManager.Start))]
    private static void Postfix_CVRPortalManager_Start(ref CVRPortalManager __instance)
    {
        // parent portal to the object below it using a physics cast
        Transform portalTransform = __instance.transform;
        Vector3 origin = portalTransform.position;
        Vector3 direction = Vector3.down;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 0.5f)) 
            portalTransform.SetParent(hit.transform);
    }
}

#endif