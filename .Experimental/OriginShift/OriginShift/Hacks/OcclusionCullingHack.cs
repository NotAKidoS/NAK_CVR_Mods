using HarmonyLib;
using UnityEngine;

namespace NAK.OriginShift.Hacks;

#region Harmony Patches

internal static class OcclusionCullingPatches
{
    // i wish i had this when working on the head hiding & shadow clones... would have had a much better
    // method of hiding mesh that wouldn't have broken magica cloth :< (one day: make test mod to do that)
    
    [HarmonyPostfix] // after all onprecull listeners
    [HarmonyPatch(typeof(Camera), "FireOnPreCull")]
    private static void OnPreCullPostfix(Camera cam)
    {
        OcclusionCullingHack hackInstance = cam.GetComponent<OcclusionCullingHack>();
        if (hackInstance != null) hackInstance.OnPostFirePreCull(cam);
    }
    
    [HarmonyPrefix] // before all onprerender listeners
    [HarmonyPatch(typeof(Camera), "FireOnPreRender")]
    private static void OnPreRenderPrefix(Camera cam)
    {
        OcclusionCullingHack hackInstance = cam.GetComponent<OcclusionCullingHack>();
        if (hackInstance != null) hackInstance.OnPreFirePreRender(cam);
    }
}

#endregion Harmony Patches

/// <summary>
/// Attempted hack to fix occlusion culling for *static* objects. This does not fix dynamic objects, they will be culled
/// by the camera's frustum & original baked occlusion culling data. Nothing can be done about that. :>
/// </summary>
public class OcclusionCullingHack : MonoBehaviour
{
    private Vector3 originalPosition;
    
    internal void OnPostFirePreCull(Camera cam)
    {
        originalPosition = cam.transform.position;
        cam.transform.position = OriginShiftManager.GetAbsolutePosition(originalPosition);
    }

    internal void OnPreFirePreRender(Camera cam)
    {
        cam.transform.position = originalPosition;
    }
}