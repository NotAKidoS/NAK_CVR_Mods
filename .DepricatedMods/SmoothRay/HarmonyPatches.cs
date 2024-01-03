using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.SmoothRay.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {

    }
}