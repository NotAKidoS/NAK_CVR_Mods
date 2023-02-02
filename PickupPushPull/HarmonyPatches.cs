using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using UnityEngine;
using PickupPushPull.InputModules;

namespace PickupPushPull.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    //uses code from https://github.com/ljoonal/CVR-Plugins/tree/main/RotateIt
    //GPL-3.0 license - Thank you ljoonal for being smart brain :plead:
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRPickupObject), "Update")]
    public static void GrabbedObjectPatch(ref CVRPickupObject __instance)
    {
        // Need to only run when the object is grabbed by the local player
        if (__instance._controllerRay == null) return;

        //and only if its a prop we support
        if (__instance.gripType == CVRPickupObject.GripType.Origin) return;

        Quaternion originalRotation = __instance.transform.rotation;
        Transform referenceTransform = __instance._controllerRay.transform;

        __instance.transform.RotateAround(__instance.transform.position, referenceTransform.right, PickupPushPull_Module.Instance.objectRotation.y * Time.deltaTime);
        __instance.transform.RotateAround(__instance.transform.position, referenceTransform.up, PickupPushPull_Module.Instance.objectRotation.x * Time.deltaTime);

        // Add the new difference between the og rotation and our newly added rotation the the stored offset.
        __instance.initialRotationalOffset *= Quaternion.Inverse(__instance.transform.rotation) * originalRotation;
    }
}