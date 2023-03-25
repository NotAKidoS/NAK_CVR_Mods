using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using UnityEngine;
using ABI_RC.Core.Player;

namespace NAK.Melons.BadAnimatorFix.HarmonyPatches;

internal static class AnimatorPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Post_PlayerSetup_Start()
    {
        BadAnimatorFixManager.OnPlayerLoaded();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRAvatar), "Start")]
    private static void Post_CVRAvatar_Start(CVRAvatar __instance)
    {
        if (!BadAnimatorFixMod.EntryCVRAvatar.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSpawnable), "Start")]
    private static void Post_CVRSpawnable_Start(CVRSpawnable __instance)
    {
        if (!BadAnimatorFixMod.EntryCVRSpawnable.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    // Set QM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance)
    {
        if (!BadAnimatorFixMod.EntryMenus.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    // Set MM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance)
    {
        if (!BadAnimatorFixMod.EntryMenus.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    private static void AddBadAnimatorFixComponentIfAnimatorExists(GameObject gameObject)
    {
        Animator[] animators = gameObject.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator.gameObject.GetComponent<BadAnimatorFix>() != null) continue;
            if (animator.runtimeAnimatorController != null)
                animator.gameObject.AddComponent<BadAnimatorFix>();
        }
    }
}
