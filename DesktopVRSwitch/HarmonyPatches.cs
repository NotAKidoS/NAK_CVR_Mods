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

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        if (CheckVR.Instance != null)
        {
            CheckVR.Instance.gameObject.AddComponent<DesktopVRSwitcher>();
            return;
        }
        __instance.gameObject.AddComponent<DesktopVRSwitcher>();
        DesktopVRSwitch.Logger.Error("CheckVR not found. Reverting to fallback method. This should never happen!");
    }
}

internal class MovementSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MovementSystem), "Start")]
    private static void Postfix_MovementSystem_Start(ref MovementSystem __instance)
    {
        __instance.gameObject.AddComponent<MovementSystemTracker>();
    }
}

internal class CVRPickupObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), "Start")]
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

internal class CVRWorldPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), "SetDefaultCamValues")]
    private static void CVRWorld_SetDefaultCamValues_Postfix()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), "CopyRefCamValues")]
    private static void CVRWorld_CopyRefCamValues_Postfix()
    {
        ReferenceCameraPatch.OnWorldLoad();
    }
}

internal class CameraFacingObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraFacingObject), "Start")]
    private static void Postfix_CameraFacingObject_Start(ref CameraFacingObject __instance)
    {
        __instance.gameObject.AddComponent<CameraFacingObjectTracker>();
    }
}

internal class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "Start")]
    private static void Postfix_IKSystem_Start(ref IKSystem __instance)
    {
        __instance.gameObject.AddComponent<IKSystemTracker>();
    }

    [HarmonyPostfix] //lazy fix so i dont need to wait few frames
    [HarmonyPatch(typeof(TrackingPoint), "Initialize")]
    private static void Postfix_TrackingPoint_Initialize(ref TrackingPoint __instance)
    {
        __instance.referenceTransform.localScale = Vector3.one;
    }
    [HarmonyPostfix] //lazy fix so device indecies can change properly
    [HarmonyPatch(typeof(SteamVRTrackingModule), "ModuleDestroy")]
    private static void Postfix_SteamVRTrackingModule_ModuleDestroy(ref SteamVRTrackingModule __instance)
    {
        for (int i = 0; i < __instance.TrackingPoints.Count; i++)
        {
            UnityEngine.Object.Destroy(__instance.TrackingPoints[i].referenceGameObject);
        }
        __instance.TrackingPoints.Clear();
    }
}

internal class VRTrackerManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VRTrackerManager), "Start")]
    private static void Postfix_VRTrackerManager_Start(ref VRTrackerManager __instance)
    {
        __instance.gameObject.AddComponent<VRTrackerManagerTracker>();
    }
}