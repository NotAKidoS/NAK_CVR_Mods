using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;

namespace NAK.ControllerFreeze.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Update))]
    static void Postfix_PlayerSetup_Update()
    {
        if (MetaPort.Instance.isUsingVr)
        {
            BodySystem.TrackingLeftArmEnabled = true;
            BodySystem.TrackingRightArmEnabled = true;
        }
    }
}