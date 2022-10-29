using ABI.CCK.Components;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

//Thanks Ben! I was scared of transpiler so I reworked a bit...

namespace DesktopVRSwitch.Patches;

[HarmonyPatch]
internal class CVRPickupObject_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPickupObject), "Start")]
    private static void CVRPickupObject_Start_Prefix(ref CVRPickupObject __instance)
    {
        if (__instance.gripOrigin == null) return;

        Transform desktopOrigin = __instance.gripOrigin.Find("[Desktop]");
        if (desktopOrigin == null) return;

        var pickupTracker = __instance.GetComponent<CVRPickupObjectTracker>();
        if (pickupTracker != null) return;

        __instance.gameObject.AddComponent<CVRPickupObjectTracker>();

        StorePreviousPosition(__instance, (!MetaPort.Instance.isUsingVr) ? __instance.gripOrigin : desktopOrigin);
    }

    private static void StorePreviousPosition(CVRPickupObject pickupObject, Transform gripOrigin)
    {
        MelonLogger.Msg("Storing previous gripOrigin.");
        CVRPickupObjectTracker.previousGripOrigin[pickupObject] = gripOrigin;
    }
}

public class CVRPickupObjectTracker : MonoBehaviour
{
    //maybe i should store both transforms instead and getcomponent for CVRPickupObject..?
    public static Dictionary<CVRPickupObject, Transform> previousGripOrigin = new();

    public void OnSwitch()
    {
        var pickupObject = GetComponent<CVRPickupObject>();

        if (pickupObject != null)
        {
            if (pickupObject.IsGrabbedByMe() && pickupObject._controllerRay != null) pickupObject._controllerRay.DropObject(true);
            (previousGripOrigin[pickupObject], pickupObject.gripOrigin) = (pickupObject.gripOrigin, previousGripOrigin[pickupObject]);
        }
    }

    private void OnDestroy()
    {
        var pickupObject = GetComponent<CVRPickupObject>();

        if (pickupObject != null)
            previousGripOrigin.Remove(pickupObject);
    }
}
