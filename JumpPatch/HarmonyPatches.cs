using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;

namespace NAK.JumpPatch.HarmonyPatches;

class MovementSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MovementSystem), "Update")]
    private static void Prefix_MovementSystem_Update(ref bool ____isGrounded)
    {
        CVRInputManager.Instance.jump = CVRInputManager.Instance.jump && ____isGrounded;
    }
}