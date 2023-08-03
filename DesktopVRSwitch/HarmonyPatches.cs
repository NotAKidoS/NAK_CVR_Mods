using ABI.CCK.Components;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using cohtml;
using cohtml.Net;
using HarmonyLib;
using NAK.DesktopVRSwitch.Patches;
using NAK.DesktopVRSwitch.VRModeTrackers;
using UnityEngine;
using Valve.VR;

namespace NAK.DesktopVRSwitch.HarmonyPatches;

internal class CheckVRPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CheckVR), nameof(CheckVR.Awake))]
    private static void Postfix_CheckVR_Start(ref CheckVR __instance)
    {
        try
        {
            __instance.gameObject.AddComponent<VRModeSwitchManager>();
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error during the patched method {nameof(Postfix_CheckVR_Start)}");
            DesktopVRSwitch.Logger.Error(e);
        }
    }
}

internal class IKSystemPatches
{
    [HarmonyPostfix] //lazy fix so device indices can change properly
    [HarmonyPatch(typeof(SteamVRTrackingModule), nameof(SteamVRTrackingModule.ModuleDestroy))]
    private static void Postfix_SteamVRTrackingModule_ModuleDestroy(ref SteamVRTrackingModule __instance)
    {
        try
        {
            foreach (TrackingPoint t in __instance.TrackingPoints)
            {
                UnityEngine.Object.Destroy(t.referenceGameObject);
            }

            __instance.TrackingPoints.Clear();
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error during the patched method {nameof(Postfix_SteamVRTrackingModule_ModuleDestroy)}");
            DesktopVRSwitch.Logger.Error(e);
        }
    }
}

internal class CVRWorldPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.SetDefaultCamValues))]
    [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.CopyRefCamValues))]
    private static void Postfix_CVRWorld_HandleCamValues()
    {
        try
        {
            ReferenceCameraPatch.OnWorldLoad();
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error during the patched method {nameof(Postfix_CVRWorld_HandleCamValues)}");
            DesktopVRSwitch.Logger.Error(e);
        }
    }
}

internal class CameraFacingObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraFacingObject), nameof(CameraFacingObject.Start))]
    private static void Postfix_CameraFacingObject_Start(ref CameraFacingObject __instance)
    {
        try
        {
            __instance.gameObject.AddComponent<CameraFacingObjectTracker>();
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error during the patched method {nameof(Postfix_CameraFacingObject_Start)}");
            DesktopVRSwitch.Logger.Error(e);
        }
    }
}

internal class CVRPickupObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Start))]
    private static void Prefix_CVRPickupObject_Start(ref CVRPickupObject __instance)
    {
        try
        {
            if (__instance.gripType == CVRPickupObject.GripType.Free)
                return;

            Transform vrOrigin = __instance.gripOrigin;
            Transform desktopOrigin = vrOrigin?.Find("[Desktop]");
            if (vrOrigin != null && desktopOrigin != null)
            {
                CVRPickupObjectTracker tracker = __instance.gameObject.AddComponent<CVRPickupObjectTracker>();
                tracker._pickupObject = __instance;
                tracker._storedGripOrigin = (!MetaPort.Instance.isUsingVr ? vrOrigin : desktopOrigin);
            }
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error during the patched method {nameof(Prefix_CVRPickupObject_Start)}");
            DesktopVRSwitch.Logger.Error(e);
        }
    }
}

internal class CohtmlUISystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UISystem), nameof(UISystem.RegisterGamepad))]
    [HarmonyPatch(typeof(UISystem), nameof(UISystem.UnregisterGamepad))]
    [HarmonyPatch(typeof(UISystem), nameof(UISystem.UpdateGamepadState))]
    private static bool Prefix_UISystem_FuckOff()
    {
        /**
            GameFace Version 1.34.0.4 – released 10 Nov 2022
            Fixed a crash when registering and unregistering gamepads
            Fix	Fixed setting a gamepad object when creating GamepadEvent from JavaScript
            Fix	Fixed a crash when unregistering a gamepad twice
            Fix	Fixed a GamepadEvent related crash during garbage collector tracing

            it is not fixed you fucking piece of shit
        **/

        // dont
        return false;
    }
}

internal class SteamVRBehaviourPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamVR_Behaviour), nameof(SteamVR_Behaviour.OnQuit))]
    private static bool Prefix_SteamVR_Behaviour_OnQuit()
    {
        if (!ModSettings.EntrySwitchToDesktopOnExit.Value) 
            return true;
        
        // If we don't switch fast enough, SteamVR will force close.
        // World Transition might cause issues. Might need to override.
        VRModeSwitchManager.Instance?.AttemptSwitch();
        return false;
    }
}