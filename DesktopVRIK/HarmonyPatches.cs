using ABI_RC.Core.Player;
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
        Small Cheese- Emotes are angled wrong due to maxRootAngle..???

    Custom knee bend normal is needed for avatars that scale incredibly small. Using animated knee bend will cause
    knees to bend in completely wrong directions. We turn it off though when in crouch/prone, as it can bleed
    into animations.

	Most other avatars play just fine.

**/

namespace NAK.DesktopVRIK.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.gameObject.AddComponent<DesktopVRIKSystem>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatarDesktop))]
    static void Postfix_PlayerSetup_SetupAvatarDesktop(ref Animator ____animator)
    {
        // only intercept if DesktopVRIK is being used
        if (DesktopVRIKSystem.Instance != null)
        {
            DesktopVRIKSystem.Instance.OnSetupAvatarDesktop(____animator);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Update))]
    static void Postfix_PlayerSetup_Update(ref bool ____emotePlaying)
    {
        // only intercept if DesktopVRIK is being used
        if (DesktopVRIKSystem.Instance?.avatarVRIK != null)
        {
            DesktopVRIKSystem.Instance.OnPlayerSetupUpdate(____emotePlaying);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupIKScaling))]
    private static bool Prefix_PlayerSetup_SetupIKScaling(float height, ref Vector3 ___scaleDifference)
    {
        // only intercept if DesktopVRIK is being used
        if (DesktopVRIKSystem.Instance?.avatarVRIK != null)
        {
            DesktopVRIKSystem.Instance.OnSetupIKScaling(1f + ___scaleDifference.y);
            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetSitting))]
    static void Postfix_PlayerSetup_SetSitting()
    {
        // only intercept if DesktopVRIK is being used
        if (DesktopVRIKSystem.Instance?.avatarVRIK != null)
        {
            DesktopVRIKSystem.Instance.OnPlayerSetupSetSitting();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ResetIk))]
    static bool Prefix_PlayerSetup_ResetIk()
    {
        // only intercept if DesktopVRIK is being used
        if (DesktopVRIKSystem.Instance?.avatarVRIK != null)
        {
            DesktopVRIKSystem.Instance.OnPlayerSetupResetIk();
            return false;
        }

        return true;
    }
}