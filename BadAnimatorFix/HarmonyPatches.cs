using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using UnityEngine;

namespace NAK.Melons.BadAnimatorFix.HarmonyPatches;

internal class AnimatorPatches
{
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRWorld), "Start")]
    private static void Post_CVRWorld_Start(CVRWorld __instance)
    {
        if (!BadAnimatorFixMod.EntryCVRWorld.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    //Set QM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance, ref GameObject ____leftVrAnchor)
    {
        if (!BadAnimatorFixMod.EntryMenus.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    //Set MM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance)
    {
        if (!BadAnimatorFixMod.EntryMenus.Value) return;
        AddBadAnimatorFixComponentIfAnimatorExists(__instance.gameObject);
    }

    private static void AddBadAnimatorFixComponentIfAnimatorExists(GameObject gameObject)
    {
        if (!BadAnimatorFixMod.EntryEnabled.Value) return;
        Animator[] animators = gameObject.GetComponentsInChildren<Animator>();
        foreach (Animator animator in animators.Where(a => a.gameObject.GetComponent<BadAnimatorFix>() == null))
        {
            if (animator.runtimeAnimatorController != null)
                animator.gameObject.AddComponent<BadAnimatorFix>();
        }
    }
}

