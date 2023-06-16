using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using NAK.DesktopVRSwitch.Patches;
using UnityEngine;

namespace NAK.DesktopVRSwitch.HarmonyPatches;

class CheckVRPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CheckVR), nameof(CheckVR.Start))]
    private static void Postfix_CheckVR_Start(ref CheckVR __instance)
    {
        __instance.gameObject.AddComponent<DesktopVRSwitcher>();
    }
}

class MovementSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MovementSystem), nameof(MovementSystem.Start))]
    private static void Postfix_MovementSystem_Start(ref MovementSystem __instance)
    {
        __instance.gameObject.AddComponent<MovementSystemTracker>();
    }
}

class CVRPickupObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Start))]
    private static void Prefix_CVRPickupObject_Start(ref CVRPickupObject __instance)
    {
        if (__instance.gripType == CVRPickupObject.GripType.Free) return;

        Transform vrOrigin = __instance.gripOrigin;
        Transform desktopOrigin = __instance.gripOrigin.Find("[Desktop]");
        if (vrOrigin != null && desktopOrigin != null)
        {
            var tracker = __instance.gameObject.AddComponent<CVRPickupObjectTracker>();
            tracker.pickupObject = __instance;
            tracker.storedGripOrigin = (!MetaPort.Instance.isUsingVr ? vrOrigin : desktopOrigin);
        }
    }
}

class CVRWorldPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.SetDefaultCamValues))]
    private static void Postfix_CVRWorld_SetDefaultCamValues()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.CopyRefCamValues))]
    private static void Postfix_CVRWorld_CopyRefCamValues()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }
}

class CameraFacingObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraFacingObject), nameof(CameraFacingObject.Start))]
    private static void Postfix_CameraFacingObject_Start(ref CameraFacingObject __instance)
    {
        __instance.gameObject.AddComponent<CameraFacingObjectTracker>();
    }
}

class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.Start))]
    private static void Postfix_IKSystem_Start(ref IKSystem __instance)
    {
        __instance.gameObject.AddComponent<IKSystemTracker>();
    }

    [HarmonyPostfix] //lazy fix so i dont need to wait few frames
    [HarmonyPatch(typeof(TrackingPoint), nameof(TrackingPoint.Initialize))]
    private static void Postfix_TrackingPoint_Initialize(ref TrackingPoint __instance)
    {
        __instance.referenceTransform.localScale = Vector3.one;
    }

    [HarmonyPostfix] //lazy fix so device indecies can change properly
    [HarmonyPatch(typeof(SteamVRTrackingModule), nameof(SteamVRTrackingModule.ModuleDestroy))]
    private static void Postfix_SteamVRTrackingModule_ModuleDestroy(ref SteamVRTrackingModule __instance)
    {
        for (int i = 0; i < __instance.TrackingPoints.Count; i++)
        {
            UnityEngine.Object.Destroy(__instance.TrackingPoints[i].referenceGameObject);
        }
        __instance.TrackingPoints.Clear();
    }
}

class VRTrackerManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VRTrackerManager), nameof(VRTrackerManager.Start))]
    private static void Postfix_VRTrackerManager_Start(ref VRTrackerManager __instance)
    {
        __instance.gameObject.AddComponent<VRTrackerManagerTracker>();
    }
}