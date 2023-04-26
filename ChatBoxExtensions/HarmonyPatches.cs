using ABI_RC.Core.Savior;
using HarmonyLib;

namespace NAK.ChatBoxExtensions.HarmonyPatches;

public class CVRInputManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), "Start")]
    static void Postfix_CVRInputManager_Start(ref CVRInputManager __instance)
    {
        ChatBoxExtensions.InputModule = __instance.gameObject.AddComponent<InputModules.InputModuleChatBoxExtensions>();
    }
}
