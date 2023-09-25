using ABI.CCK.Components;
using ABI_RC.Core.Player;
using HarmonyLib;
using NAK.DesktopVRIK.IK;
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

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.gameObject.AddComponent<IKManager>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatarDesktop))]
    private static void Postfix_PlayerSetup_SetupAvatarDesktop(ref PlayerSetup __instance)
    {
        if (!ModSettings.EntryEnabled.Value)
            return;

        try
        {
            IKManager.Instance?.OnAvatarInitialized(__instance._avatar);
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatarDesktop)}");
            DesktopVRIK.Logger.Error(e);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ClearAvatar))]
    private static void Postfix_PlayerSetup_ClearAvatar()
    {
        try
        {
            IKManager.Instance?.OnAvatarDestroyed();
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_ClearAvatar)}");
            DesktopVRIK.Logger.Error(e);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupIKScaling))]
    private static void Prefix_PlayerSetup_SetupIKScaling(ref Vector3 ___scaleDifference, ref bool __runOriginal)
    {
        try
        {
            if (IKManager.Instance != null)
                __runOriginal = !IKManager.Instance.OnPlayerScaled(1f + ___scaleDifference.y);
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Prefix_PlayerSetup_SetupIKScaling)}");
            DesktopVRIK.Logger.Error(e);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetSitting))]
    private static void Postfix_PlayerSetup_SetSitting(ref bool ___isCurrentlyInSeat)
    {
        try
        {
            IKManager.Instance?.OnPlayerSeatedStateChanged(___isCurrentlyInSeat);
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetSitting)}");
            DesktopVRIK.Logger.Error(e);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ResetIk))]
    private static void Prefix_PlayerSetup_ResetIk(ref PlayerSetup __instance, ref bool __runOriginal)
    {
        try
        {
            __runOriginal = true;
            if (IKManager.Instance == null || !IKManager.Instance.IsAvatarInitialized())
                return;

            CVRMovementParent currentParent = __instance._movementSystem._currentParent;
            if (currentParent != null && currentParent._referencePoint != null)
                IKManager.Instance.OnPlayerHandleMovementParent(currentParent);
            else
                IKManager.Instance.OnPlayerTeleported();
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Prefix_PlayerSetup_ResetIk)}");
            DesktopVRIK.Logger.Error(e);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Update))]
    private static void Postfix_PlayerSetup_Update()
    {
        try
        {
            IKManager.Instance?.OnPlayerUpdate();
        }
        catch (Exception e)
        {
            DesktopVRIK.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_Update)}");
            DesktopVRIK.Logger.Error(e);
        }
    }
}