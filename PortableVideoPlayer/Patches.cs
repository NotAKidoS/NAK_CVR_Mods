using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.VideoPlayer.Scripts;
using HarmonyLib;
using UnityEngine;

namespace NAK.PortableVideoPlayer.Patches;

internal static class CVR_MenuManager_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), nameof(CVR_MenuManager.UILibInit))]
    private static void Postfix_CVR_MenuManager_UILibInit()
        => PortableVideoPlayerMod.SetupQuickMenu();
}

internal static class ViewManagerVideoPlayer_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManagerVideoPlayer), nameof(ViewManagerVideoPlayer.E_TimeUpdated))]
    private static void Postfix_ViewManagerVideoPlayer_E_TimeUpdated(ViewManagerVideoPlayer __instance, double __0, double __1, bool __2)
    {
        PortableVideoPlayerController controller = PortableVideoPlayerController.Instance;
        if (controller is null || !ReferenceEquals(controller.LinkedUi, __instance))
            return;

        controller.OnTimeUpdated(__0, __1, __2);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManagerVideoPlayer), nameof(ViewManagerVideoPlayer.E_PlayerStateChanged))]
    private static void Postfix_ViewManagerVideoPlayer_E_PlayerStateChanged(ViewManagerVideoPlayer __instance)
    {
        PortableVideoPlayerController controller = PortableVideoPlayerController.Instance;
        if (controller is null || !ReferenceEquals(controller.LinkedUi, __instance))
            return;

        controller.SyncState();
    }
}

internal static class PickupUiElement_Patches
{
    /// <summary> The angle threshold to align the up vector to world up. </summary>
    private const float UpAlignmentThreshold = 15f;

    private static bool IsPortablePlayerPickup(PickupUiElement pickup)
    {
        PortableVideoPlayerController controller = PortableVideoPlayerController.Instance;
        return controller is not null && ReferenceEquals(controller.Pickup, pickup);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PickupUiElement), nameof(PickupUiElement.HandlePickupSnapping))]
    private static bool Prefix_PickupUiElement_HandlePickupSnapping(PickupUiElement __instance)
        => !IsPortablePlayerPickup(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PickupUiElement), nameof(PickupUiElement.HandlePickupMovement))]
    private static bool Prefix_PickupUiElement_HandlePickupMovement(PickupUiElement __instance)
    {
        if (!IsPortablePlayerPickup(__instance))
            return true;

        Transform rayTransform = __instance._rayTransform;
        Transform rootTransform = __instance._rootTransform;
        Transform lookAtTransform = __instance._lookAtTransform;

        Vector3 targetPosition = rayTransform.position + rayTransform.TransformDirection(__instance._initialPositionOffset);
        Quaternion targetRotation = rayTransform.rotation;

        if (lookAtTransform)
        {
            rayTransform.rotation = rootTransform.rotation;

            Vector3 lookDir = (__instance._invertLookAtDirection ? -1f : 1f) * (lookAtTransform.position - rootTransform.position);
            targetRotation = Quaternion.Slerp(targetRotation, Quaternion.LookRotation(lookDir, GetTargetUp()), __instance._lookAtWeight);
        }

        float positionSpeed = __instance._positionInterpolationSpeed;
        float rotationSpeed = __instance._rotationInterpolationSpeed;

        rootTransform.SetPositionAndRotation(
            positionSpeed > 0f ? Vector3.Lerp(rootTransform.position, targetPosition, positionSpeed * Time.deltaTime) : targetPosition,
            rotationSpeed > 0f ? Quaternion.Lerp(rootTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime) : targetRotation);

        return false;
    }

    private static Vector3 GetTargetUp()
    {
        PlayerSetup playerSetup = PlayerSetup.Instance;
        Vector3 playSpaceUp = playerSetup.transform.up;
        Vector3 playerCamUp = playerSetup.activeCam.transform.up;
        return Vector3.Angle(playSpaceUp, playerCamUp) > UpAlignmentThreshold ? playerCamUp : playSpaceUp;
    }
}
