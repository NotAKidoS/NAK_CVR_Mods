using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;

namespace NAK.JumpPatch.HarmonyPatches;

class MovementSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MovementSystem), nameof(MovementSystem.Update))]
    private static void Prefix_MovementSystem_Update(ref bool ____isGrounded, ref bool __state)
    {
        __state = CVRInputManager.Instance.jump;
        CVRInputManager.Instance.jump = CVRInputManager.Instance.jump && ____isGrounded;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MovementSystem), nameof(MovementSystem.Update))]
    private static void Postfix_MovementSystem_Update(ref bool __state)
    {
        CVRInputManager.Instance.jump = __state;
    }
}