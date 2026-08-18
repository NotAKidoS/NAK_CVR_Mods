using System.Collections;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Systems.ChatBox;
using DarkRift;
using HarmonyLib;
using NAK.CleanPlates.Helpers;
using NAK.CleanPlates.Network;
using UnityEngine;

namespace NAK.CleanPlates.Patches;

internal static class PlayerNameplate_Patches
{
    // The rank update path calls this on plates we never let start so it would
    // explode.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerNameplate), nameof(PlayerNameplate.UpdateNamePlateSettings))]
    private static bool Prefix_PlayerNameplate_UpdateNamePlateSettings() => false;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerNameplate), nameof(PlayerNameplate.Start))]
    private static bool Prefix_PlayerNameplate_Start(ref PlayerNameplate __instance)
    {
        // Kill native plate.
        __instance.contentGo.SetActive(false);
        __instance.gameObject.SetActive(false);
        __instance.enabled = false;
        return false;
    }
}

internal static class CameraIndicatorPlate_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CameraIndicatorPlate), nameof(CameraIndicatorPlate.Start))]
    private static bool Prefix_CameraIndicatorPlate_Start(ref CameraIndicatorPlate __instance)
    {
        // Kick off creating our own plate.
        PlateManager.OnCameraPlateStart(__instance);

        __instance.nameplateGameObjectWrapper.SetActive(false);
        __instance.enabled = false;
        return false;
    }
}

internal static class CVRObjectLoader_Patches
{
    [HarmonyPostfix] // postfix for a coroutine wrapper, make it make sense :D
    [HarmonyPatch(typeof(CVRObjectLoader.LoadedObject), nameof(CVRObjectLoader.LoadedObject.PostProcessAsset))]
    private static void Wrap_CVRObjectLoader_LoadedObject_PostProcessAsset(
        CVRObjectLoader.LoadedObject __instance, ref IEnumerator __result)
        => __result = CVRObjectLoader_LoadedObject_PostProcessAsset(__instance, __result);

    // Appending our own post-processing logic to post-bundle load to timeslice process
    // avatars to calculate an optimial nameplate height from head bone ONE TIME per-bundle.
    // I wrap this in like 3 mods so surely it's all ok :)
    private static IEnumerator CVRObjectLoader_LoadedObject_PostProcessAsset(
        CVRObjectLoader.LoadedObject loadedObject,
        IEnumerator original)
    {
        // Run original
        while (original.MoveNext()) yield return original.Current;

        // Process any avatar for nameplate heights. Creates a marker component
        // on the avatar root with it stored for us to use later.
        if (loadedObject.type == CVRObjectLoader.ObjectType.Avatar)
            yield return NameplateAnchorUtility.BakeRoutine(loadedObject.gameObject);
    }
}

internal static class OverheadController_Patches
{
    // Last priority to run after TW:
    // https://github.com/TotallyWholesome/TotallyWholesomeMod/blob/19403286a812b1ab5dd7c83e2e83cbed3cda3837/TotallyWholesome/Managers/Status/StatusManager.cs#L270
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(OverheadController), nameof(OverheadController.Start))]
    [HarmonyPriority(Priority.Last)]
    private static void Prefix_OverheadController_Start(ref OverheadController __instance)
    {
        // PlateManager drives the positioning for every plate off one lifetime
        // event. This component would do its own in Update, per nameplate.
        // (many components defining Update/LateUpdate and doing fuck all stacks up and stalls main)
        __instance.enabled = false;
        
        // Instantiate our own plate. Listening here instead of PlayerNameplate
        // specifically because this is also where TW patches.
        PlateManager.OnOverheadControllerStart(__instance);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(OverheadController), nameof(OverheadController.Update))]
    private static void Prefix_OverheadController_Update(ref OverheadController __instance) 
        => __instance.enabled = false; // idk, had this occur consistently
}

internal static class NetIKController_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetIKController), nameof(NetIKController.UpdateColliderHeight))]
    private static void Postfix_NetIKController_UpdateColliderHeight(ref NetIKController __instance)
    {
        // Tell the nameplate anchor for this players avatar to update its cached heights.
        GameObject avatar = __instance._remoteAvatar;
        if (avatar && avatar.TryGetComponent(out NameplateAnchor anchor))
            anchor.UpdateCachedHeightsOnScaleChange();
    }
}

internal static class ChatBoxBubbleBehavior_Patches
{
    // Gut the native bubble entirely, we drive our own off ChatBoxAPI.
    // Start never runs so it never registers itself or touches its materials.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatBoxBubbleBehavior), nameof(ChatBoxBubbleBehavior.Start))]
    private static bool Prefix_ChatBoxBubbleBehavior_Start(ref ChatBoxBubbleBehavior __instance)
    {
        __instance.enabled = false;
        return false;
    }

    // Player is only assigned in Start, so the OnDestroy would explode.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatBoxBubbleBehavior), nameof(ChatBoxBubbleBehavior.OnDestroy))]
    private static bool Prefix_ChatBoxBubbleBehavior_OnDestroy() => false;
}

internal static class CVRPlayerManager_Patches
{
    // Reimplemented so only the plates in the message get touched, and so it
    // stops poking UpdateNamePlateSettings on plates we never let start.
    // This is the only patch that explodes on GS2 branch :D
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRPlayerManager), nameof(CVRPlayerManager.UpdatePlayerRankInformation))]
    private static bool Prefix_CVRPlayerManager_UpdatePlayerRankInformation(CVRPlayerManager __instance, Message message)
    {
        try
        {
            using DRMessageHelper msg = new(message);

            msg.Read(out int total);
            for (int i = 0; i < total; i++)
            {
                msg.Read(out string uuid);
                msg.Read(out string abbreviation);
                msg.Read(out string fullname);

                msg.Read(out byte colorR);
                msg.Read(out byte colorG);
                msg.Read(out byte colorB);
                msg.Read(out byte colorA);

                PlayerBase player = null;
                if (MetaPort.Instance.ownerId == uuid)
                    player = PlayerSetup.Instance;
                else if (__instance.UserIdToPlayerEntity.TryGetValue(uuid, out CVRPlayerEntity playerEntity))
                    player = playerEntity.PuppetMaster;

                if (player == null) continue;

                PlayerDescriptor descriptor = player.playerDescriptor;
                // The native handler we replace writes the legacy field too, and
                // anything still reading it has to keep agreeing with the new info.
#pragma warning disable CS0618
                descriptor.userRank = fullname;
#pragma warning restore CS0618
                descriptor.RankInfoFromGameServer = new PlayerDescriptor.NameplateRankInfo
                {
                    FullName = fullname,
                    Abbreviation = abbreviation,
                    DisplayColor = new Color32(colorR, colorG, colorB, colorA),
                };

                PlateManager.RefreshRank(player);
            }
        }
        catch (Exception e)
        {
            CleanPlatesMod.Logger.Error("Unable to update player rank:", e);
        }

        return false;
    }
}

internal static class ViewManager_Patches
{
    // public void NotifyUser(string category, string title, float time)
    // NotifyUser("Profile", "Successfully updated your bio!", 5f);
    // NotifyUser("Profile", "Successfully updated your pronouns!", 5f);
    // NotifyUser("Profile", "Successfully uploaded profile pic!", 5f);
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.NotifyUser))]
    private static void Postfix_ViewManager_NotifyUser(string category, string title, float time)
    {
        if (category != "Profile") return;
        if (title is not "Successfully updated your pronouns!"
            and not "Successfully uploaded profile pic!") return;

        PlateManager.RefreshProfile(PlayerSetup.Instance);
        CleanPlatesNetwork.SendProfileUpdate();
    }

    // Hooking this instead of AuthManager.Authenticated for compat with
    // AccountSwitcher mod (doesn't fire the event as it breaks expectations elsewhere).
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.UserLoggedIn))]
    private static void Postfix_ViewManager_UserLoggedIn()
        => PlateManager.OnAuthenticated();
}