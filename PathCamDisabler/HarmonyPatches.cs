using ABI_RC.Core.IO;
using ABI_RC.Systems.InputManagement.InputModules;
using HarmonyLib;

namespace NAK.PathCamDisabler.HarmonyPatches;

internal class CVRPathCamControllerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPathCamController), nameof(CVRPathCamController.Update))]
    private static void Prefix_CVRPathCamController_Update(ref bool __runOriginal)
    {
        __runOriginal &= !PathCamDisabler.EntryDisablePathCam.Value;
    }
}

internal class CVRInputModule_KeyboardPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputModule_Keyboard), nameof(CVRInputModule_Keyboard.Update_Binds))]
    private static void Postfix_CVRInputModule_Keyboard_Update_Binds(ref CVRInputModule_Keyboard __instance)
    {
        if (PathCamDisabler.EntryDisableFlightBind.Value)
            __instance._inputManager.toggleFlight = false;
    }
}