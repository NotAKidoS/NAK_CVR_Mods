using ABI.CCK.Components;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using HarmonyLib;
using NAK.DesktopVRSwitch.Patches;
using NAK.DesktopVRSwitch.VRModeTrackers;
using UnityEngine;

namespace NAK.DesktopVRSwitch.HarmonyPatches;

class CheckVRPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CheckVR), nameof(CheckVR.Start))]
    static void Postfix_CheckVR_Start(ref CheckVR __instance)
    {
        __instance.gameObject.AddComponent<VRModeSwitchManager>();
    }
}

class IKSystemPatches
{
    [HarmonyPostfix] //lazy fix so i dont need to wait few frames
    [HarmonyPatch(typeof(TrackingPoint), nameof(TrackingPoint.Initialize))]
    static void Postfix_TrackingPoint_Initialize(ref TrackingPoint __instance)
    {
        __instance.referenceTransform.localScale = Vector3.one;
    }

    [HarmonyPostfix] //lazy fix so device indecies can change properly
    [HarmonyPatch(typeof(SteamVRTrackingModule), nameof(SteamVRTrackingModule.ModuleDestroy))]
    static void Postfix_SteamVRTrackingModule_ModuleDestroy(ref SteamVRTrackingModule __instance)
    {
        for (int i = 0; i < __instance.TrackingPoints.Count; i++)
        {
            UnityEngine.Object.Destroy(__instance.TrackingPoints[i].referenceGameObject);
        }
        __instance.TrackingPoints.Clear();
    }
}

class CVRWorldPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.SetDefaultCamValues))]
    static void Postfix_CVRWorld_SetDefaultCamValues()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.CopyRefCamValues))]
    static void Postfix_CVRWorld_CopyRefCamValues()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }
}

class CameraFacingObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraFacingObject), nameof(CameraFacingObject.Start))]
    static void Postfix_CameraFacingObject_Start(ref CameraFacingObject __instance)
    {
        __instance.gameObject.AddComponent<CameraFacingObjectTracker>();
    }
}