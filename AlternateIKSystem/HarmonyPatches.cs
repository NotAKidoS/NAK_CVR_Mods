using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using HarmonyLib;
using NAK.AlternateIKSystem.IK;
using UnityEngine;

namespace NAK.AlternateIKSystem.HarmonyPatches;

internal class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
    private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        __instance.gameObject.AddComponent<IKManager>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    private static void Postfix_PlayerSetup_SetupAvatar(GameObject inAvatar)
    {
        if (!ModSettings.EntryEnabled.Value)
            return;

        try
        {
            IKManager.Instance?.OnAvatarInitialized(inAvatar);
        }
        catch (Exception e)
        {
            AlternateIKSystem.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatar)}");
            AlternateIKSystem.Logger.Error(e);
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
            AlternateIKSystem.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_ClearAvatar)}");
            AlternateIKSystem.Logger.Error(e);
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
            AlternateIKSystem.Logger.Error($"Error during the patched method {nameof(Prefix_PlayerSetup_SetupIKScaling)}");
            AlternateIKSystem.Logger.Error(e);
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
            AlternateIKSystem.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetSitting)}");
            AlternateIKSystem.Logger.Error(e);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ResetIk))]
    private static void Prefix_PlayerSetup_ResetIk(ref PlayerSetup __instance, ref bool __runOriginal)
    {
        try
        {
            CVRMovementParent currentParent = __instance._movementSystem._currentParent;
            if (currentParent?._referencePoint == null)
                return;

            if (IKManager.Instance != null)
                __runOriginal = !IKManager.Instance.OnPlayerHandleMovementParent(currentParent);
        }
        catch (Exception e)
        {
            AlternateIKSystem.Logger.Error($"Error during the patched method {nameof(Prefix_PlayerSetup_ResetIk)}");
            AlternateIKSystem.Logger.Error(e);
        }
    }
}

internal class IKSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.InitializeAvatar))]
    private static void Prefix_IKSystem_InitializeAvatar(ref bool __runOriginal)
    {
        // Don't setup with native IKSystem
        __runOriginal = !ModSettings.EntryEnabled.Value;
    }
}