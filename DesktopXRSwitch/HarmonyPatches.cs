using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using NAK.Melons.DesktopXRSwitch.Patches;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.gameObject.AddComponent<PlayerSetupTracker>();
        if (CheckVR.Instance != null)
        {
            CheckVR.Instance.gameObject.AddComponent<DesktopXRSwitcher>();
            return;
        }
        __instance.gameObject.AddComponent<DesktopXRSwitcher>();
        DesktopXRSwitch.Logger.Error("CheckVR not found. Reverting to fallback method. This should never happen!");
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
}