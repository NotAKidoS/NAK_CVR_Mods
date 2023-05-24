using ABI_RC.Systems.IK;
using HarmonyLib;
using UnityEngine;
using Valve.VR;

namespace NAK.TrackedPointFix.HarmonyPatches;

class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TrackingPoint), nameof(TrackingPoint.SetVisibility))]
    static void Postfix_TrackingPoint_SetVisibility(ref TrackingPoint __instance)
    {
        GameObject systemTracker = __instance.referenceTransform.Find("DisplayTracker").gameObject;
        if (systemTracker != null)
        {
            systemTracker.SetActive(__instance.displayObject.activeSelf);
            __instance.displayObject.SetActive(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TrackingPoint), nameof(TrackingPoint.Initialize))]
    static void Postfix_TrackingPoint_Initialize(ref TrackingPoint __instance)
    {
        GameObject systemTracker = new GameObject();
        systemTracker.name = "DisplayTracker";
        systemTracker.transform.parent = __instance.referenceTransform;
        systemTracker.transform.localPosition = Vector3.zero;
        systemTracker.transform.localRotation = Quaternion.identity;
        systemTracker.transform.localScale = Vector3.one;
        systemTracker.SetActive(false);

        SteamVR_RenderModel renderModel = systemTracker.AddComponent<SteamVR_RenderModel>();
        renderModel.enabled = true;
        renderModel.updateDynamically = false;
        renderModel.createComponents = false;
        renderModel.SetDeviceIndex(ExtractNumberFromTrackingPoint(__instance.name));
    }

    public static int ExtractNumberFromTrackingPoint(string inputString)
    {
        string numberString = inputString.Replace("SteamVR_", "");
        int number = int.Parse(numberString);
        return number;
    }
}