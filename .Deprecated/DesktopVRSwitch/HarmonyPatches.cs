using ABI_RC.Core;
using ABI_RC.Systems.InputManagement;
using HarmonyLib;
using NativeVRModeSwitchManager = ABI_RC.Systems.VRModeSwitch.VRModeSwitchManager;

namespace NAK.DesktopVRSwitch.HarmonyPatches;

internal class CVRInputManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), "OnPostVRModeSwitch")]
    private static void Postfix_CVRInputManager_OnPostVRModeSwitch(bool inVr, UnityEngine.Camera playerCamera)
    {
        RootLogic.Instance.ToggleMouse(inVr);
    }
}

internal class VRModeSwitchManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NativeVRModeSwitchManager), "StartSwitchInternal")]
    private static void Postfix_CVRInputManager_OnPostVRModeSwitch()
    {
        CVRInputManager.Instance.inputEnabled = false;
    }
}