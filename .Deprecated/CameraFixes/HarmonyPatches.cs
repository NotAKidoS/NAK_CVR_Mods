using HarmonyLib;
using UnityEngine;
using static ABI_RC.Core.CVRTools;

namespace NAK.CameraFixes.HarmonyPatches;

class CVRCamControllerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRCamController), nameof(CVRCamController.Start))]
    static void Postfix_CVRCamController_Start(ref CVRCamController __instance)
    {
        SetGameObjectLayerRecursive(__instance.gameObject, LayerMask.NameToLayer("UI Internal"));
    }
}