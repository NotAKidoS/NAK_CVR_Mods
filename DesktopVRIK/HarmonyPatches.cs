using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using HarmonyLib;
using UnityEngine;

/**

	The process of calibrating VRIK is fucking painful.
	
	Avatars of Note:
		TurtleNeck Ferret- close feet, far shoulders, nonideal rig.
		Space Robot Kyle- the worst bone rolls on the planet, tpose/headikcalibration fixed it mostly... ish.
		Exteratta- knees bend backwards without proper tpose.
        Chito- left foot is far back without proper tpose & foot ik distance, was uploaded in falling anim state.
        Atlas (portal2)- Wide stance, proper feet distance needed to be calculated.
        Freddy (gmod)- Doesn't have any fingers, wristToPalmAxis & palmToThumbAxis needed to be set manually.

	Most other avatars play just fine.

**/

namespace NAK.Melons.DesktopVRIK.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarDesktop")]
    static void Postfix_PlayerSetup_SetupAvatarDesktop(ref Animator ____animator)
    {
        if (____animator != null && ____animator.avatar != null && ____animator.avatar.isHuman)
        {
            DesktopVRIK.Instance?.OnSetupAvatarDesktop();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Update")]
    static void Postfix_PlayerSetup_Update(ref bool ____emotePlaying)
    {
        DesktopVRIK.Instance?.OnPlayerSetupUpdate(____emotePlaying);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupIKScaling")]
    private static bool Prefix_PlayerSetup_SetupIKScaling(float height, ref Vector3 ___scaleDifference)
    {
        return !(bool)DesktopVRIK.Instance?.OnSetupIKScaling(1f + ___scaleDifference.y);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), "ResetIk")]
    static bool Prefix_PlayerSetup_ResetIk()
    {
        return !(bool)DesktopVRIK.Instance?.OnPlayerSetupResetIk();
    }
}

class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "Start")]
    private static void Postfix_IKSystem_Start(ref IKSystem __instance)
    {
        __instance.gameObject.AddComponent<DesktopVRIK>();
    }
}
