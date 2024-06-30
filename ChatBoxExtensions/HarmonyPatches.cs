using ABI_RC.Systems.InputManagement;
using HarmonyLib;

namespace NAK.ChatBoxExtensions.HarmonyPatches;

public class CVRInputManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), nameof(CVRInputManager.Start))]
    private static void Postfix_CVRInputManager_Start(ref CVRInputManager __instance)
    {
        __instance.AddInputModule(ChatBoxExtensions.InputModule);
    }
}
