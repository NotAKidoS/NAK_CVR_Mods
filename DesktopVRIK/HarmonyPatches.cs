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

    //[HarmonyPostfix]
    //[HarmonyPatch(typeof(PlayerSetup), "ReCalibrateAvatar")]
    //static void Postfix_PlayerSetup_ReCalibrateAvatar()
    //{
    //    DesktopVRIK.Instance?.OnReCalibrateAvatar();
    //}
}

class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "Start")]
    private static void Postfix_IKSystem_Start(ref IKSystem __instance)
    {
        __instance.gameObject.AddComponent<DesktopVRIK>();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSystem), "ApplyAvatarScaleToIk")]
    private static bool Prefix_IKSystem_ApplyAvatarScaleToIk(float height)
    {
        return !(bool)DesktopVRIK.Instance?.OnApplyAvatarScaleToIk(height);
    }
}
