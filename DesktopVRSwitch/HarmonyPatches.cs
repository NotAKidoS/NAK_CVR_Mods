using ABI.CCK.Components;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using cohtml;
using HarmonyLib;
using NAK.DesktopVRSwitch.Patches;
using NAK.DesktopVRSwitch.VRModeTrackers;
using UnityEngine;
using Valve.VR;

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
        __instance.gameObject.AddComponent<CameraFacingObjectTracker>()._cameraFacingObject = __instance;
    }
}

class CVRPickupObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Start))]
    static void Prefix_CVRPickupObject_Start(ref CVRPickupObject __instance)
    {
        if (__instance.gripType == CVRPickupObject.GripType.Free)
            return;

        Transform vrOrigin = __instance.gripOrigin;
        Transform desktopOrigin = __instance.gripOrigin.Find("[Desktop]");
        if (vrOrigin != null && desktopOrigin != null)
        {
            var tracker = __instance.gameObject.AddComponent<CVRPickupObjectTracker>();
            tracker._pickupObject = __instance;
            tracker._storedGripOrigin = (!MetaPort.Instance.isUsingVr ? vrOrigin : desktopOrigin);
        }
    }
}

class CohtmlUISystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CohtmlUISystem), nameof(CohtmlUISystem.RegisterGamepad))]
    [HarmonyPatch(typeof(CohtmlUISystem), nameof(CohtmlUISystem.UnregisterGamepad))]
    [HarmonyPatch(typeof(CohtmlUISystem), nameof(CohtmlUISystem.UpdateGamepadState))]
    static bool Prefix_CohtmlUISystem_FuckOff()
    {
        /** 
         GameFace Version 1.34.0.4 – released 10 Nov 2022
        	Fixed a crash when registering and unregistering gamepads
            Fix	Fixed setting a gamepad object when creating GamepadEvent from JavaScript
            Fix	Fixed a crash when unregistering a gamepad twice
            Fix	Fixed a GamepadEvent related crash during garbage collector tracing

            we are using 1.17.0 (released 10/09/21) :):):)
        **/

        // dont
        return false;
    }
}

class SteamVRBehaviourPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamVR_Behaviour), nameof(SteamVR_Behaviour.OnQuit))]
    static bool Prefix_SteamVR_Behaviour_OnQuit()
    {
        if (DesktopVRSwitch.EntrySwitchToDesktopOnExit.Value)
        {
            // If we don't switch fast enough, SteamVR will force close.
            // World Transition might cause issues. Might need to override.
            VRModeSwitchManager.Instance?.AttemptSwitch();
            return false;
        }
        return true;
    }
}