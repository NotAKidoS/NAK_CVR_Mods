using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.TransformHider;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Camera;
using HarmonyLib;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderUtils), nameof(TransformHiderUtils.SetupAvatar))]
    private static bool OnSetupAvatar(GameObject avatar)
    {
        if (!AvatarCloneTestMod.EntryUseAvatarCloneTest.Value) return true;
        avatar.AddComponent<AvatarClone>();
        return false;
    }
}
    