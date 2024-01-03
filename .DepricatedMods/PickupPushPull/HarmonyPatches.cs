using ABI.CCK.Components;
using HarmonyLib;
using NAK.PickupPushPull.InputModules;
using UnityEngine;

namespace NAK.PickupPushPull.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    //uses code from https://github.com/ljoonal/CVR-Plugins/tree/main/RotateIt
    //GPL-3.0 license - Thank you ljoonal for being smart brain :plead:

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRPickupObject), nameof(CVRPickupObject.Update))]
    public static void GrabbedObjectPatch(ref CVRPickupObject __instance)
    {
        if (__instance._controllerRay == null) 
            return;

        if (__instance.gripType == CVRPickupObject.GripType.Origin) 
            return;

        Quaternion originalRotation = __instance.transform.rotation;
        Transform referenceTransform = __instance._controllerRay.transform;

        __instance.transform.RotateAround(__instance.transform.position, referenceTransform.right, PickupPushPull_Module.Instance.objectRotation.y * Time.deltaTime);
        __instance.transform.RotateAround(__instance.transform.position, referenceTransform.up, PickupPushPull_Module.Instance.objectRotation.x * Time.deltaTime);

        __instance.initialRotationalOffset *= Quaternion.Inverse(__instance.transform.rotation) * originalRotation;
    }
}