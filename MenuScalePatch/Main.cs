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
        internal static bool adjustedMenuPosition = false;
        internal static void SetMenuPosition(Transform menuTransform, float scale)
        {
            Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            if (!MetaPort.Instance.isUsingVr)
            {
                menuTransform.eulerAngles = rotationPivot.eulerAngles;
            }
            menuTransform.position = rotationPivot.position + rotationPivot.forward * 1f * scale;
            adjustedMenuPosition = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), "SetScale")]
        private static void SetQMScale(ref CohtmlView ___quickMenu, ref bool ___needsQuickmenuPositionUpdate, ref float ____scaleFactor, ref GameObject ____leftVrAnchor)
        {
            if (MetaPort.Instance.isUsingVr)
            {
                ___quickMenu.transform.position = ____leftVrAnchor.transform.position;
                ___quickMenu.transform.rotation = ____leftVrAnchor.transform.rotation;
                ___needsQuickmenuPositionUpdate = false;
                return;
            }
            SetMenuPosition(___quickMenu.transform, ____scaleFactor);
            ___needsQuickmenuPositionUpdate = false;
        }

        /**
            ViewManager.SetScale runs once a second when it should only run when aspect ratio changes- CVR bug
            assuming its caused by cast from int to float getting the screen size, something floating point bleh
            attempting to ignore that call if there wasnt actually a change
        **/

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void CheckMMScale(float avatarHeight, ref float ___cachedAvatarHeight, out bool __state)
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

            SetMenuPosition(__instance.transform, ___scaleFactor);
            ___needsMenuPositionUpdate = false;
        }

        /**
            Following code resets the menu position on LateUpdate so you can use the menu while moving/falling.
            It is Desktop only. QM inputs still don't work because they do their input checks in LateUpdate???
        **/

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CVR_MenuManager), "LateUpdate")]
        private static void DesktopQMFix(ref CohtmlView ___quickMenu, ref bool ___needsQuickmenuPositionUpdate, ref float ____scaleFactor, ref bool ____quickMenuOpen)
        {
            if (MetaPort.Instance.isUsingVr) return;
            if (____quickMenuOpen && !adjustedMenuPosition)
            {
                SetMenuPosition(___quickMenu.transform, ____scaleFactor);
                ___needsQuickmenuPositionUpdate = false;
            }
            adjustedMenuPosition = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewManager), "LateUpdate")]
        private static void DesktopMMFix(ref ViewManager __instance, ref bool ___needsMenuPositionUpdate, ref float ___scaleFactor, bool __state, ref bool ____gameMenuOpen)
        {
            if (MetaPort.Instance.isUsingVr) return;
            if (____gameMenuOpen && !adjustedMenuPosition)
            {
                SetMenuPosition(__instance.transform, ___scaleFactor);
                ___needsMenuPositionUpdate = false;
            }
            adjustedMenuPosition = false;
        }
    }
}