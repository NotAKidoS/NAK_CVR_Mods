
using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using NAK.InteractionTest.Components;

namespace NAK.InteractionTest.Patches;

internal static class ControllerRayPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.Start))]
    private static void Postfix_BetterCharacterController_Start(ref ControllerRay __instance)
    {
        InteractionTracker.Setup(__instance.gameObject, __instance.hand == CVRHand.Left);
    }
}