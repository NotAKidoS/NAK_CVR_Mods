using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using cohtml;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace MenuScalePatch;

public class MenuScalePatch : MelonMod
{
    [HarmonyPatch]
    private class HarmonyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), "SetScale")]
        private static void SetQMScale(ref CohtmlView ___quickMenu, ref float ____scaleFactor, float avatarHeight)
        {
            if (!MetaPort.Instance.isUsingVr)
            {
                //correct quickmenu - pretty much needsQuickmenuPositionUpdate()
                Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
                ___quickMenu.transform.eulerAngles = new Vector3(rotationPivot.eulerAngles.x, rotationPivot.eulerAngles.y, rotationPivot.eulerAngles.z);
                ___quickMenu.transform.position = rotationPivot.position + rotationPivot.forward * 1f * ____scaleFactor;
            }
        }

        //ViewManager.SetScale runs once a second when it should only run when aspect ratio changes- CVR bug
        //assuming its caused by cast from int to float getting the screen size, something floating point bleh
        //attempting to ignore that call if there wasnt actually a change

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void CheckLegit(float avatarHeight, ref float ___cachedAvatarHeight, out bool __state)
        {
            if (___cachedAvatarHeight == avatarHeight)
            {
                __state = false;
                return;
            }
            __state = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void SetMMScale(ref ViewManager __instance, ref bool ___needsMenuPositionUpdate, ref float ___scaleFactor, bool __state)
        {
            if (!__state) return;

            //correct main menu - pretty much UpdateMenuPosition()
            Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            __instance.gameObject.transform.position = rotationPivot.position + __instance.gameObject.transform.forward * 1f * ___scaleFactor;
            ___needsMenuPositionUpdate = false;
        }
    }
}