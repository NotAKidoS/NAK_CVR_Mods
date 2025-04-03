using ABI_RC.Core;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.FuckMLA.HarmonyPatches;

internal class RootLogicPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RootLogic), nameof(RootLogic.Awake))]
    private static void Postfix_RootLogic_Awake(ref RootLogic __instance)
    {
        __instance.ToggleMouse(MetaPort.Instance.isUsingVr);
    }
}

internal class CVRInputModulePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRInputModule), nameof(CVRInputModule.Update_Look), typeof(Vector2))]
    private static bool Prefix_CVRInputModule_Update_Look()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }
}

internal class MOUSELOCKALPHAPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MOUSELOCKALPHA), nameof(MOUSELOCKALPHA.Start))]
    private static bool Prefix_MOUSELOCKALPHA_Start(ref MOUSELOCKALPHA __instance)
    {
        Object.DestroyImmediate(__instance);
        return false;
    }
}