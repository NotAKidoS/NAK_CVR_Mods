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
        private static void SetQMScale(ref CohtmlView ___quickMenu, ref float ____scaleFactor)
        {
            //correct quickmenu - pretty much needsQuickmenuPositionUpdate()
            Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            ___quickMenu.transform.eulerAngles = new Vector3(rotationPivot.eulerAngles.x, rotationPivot.eulerAngles.y, rotationPivot.eulerAngles.z);
            ___quickMenu.transform.position = rotationPivot.position + rotationPivot.forward * 1f * ____scaleFactor;
        }

        //ViewManager.SetScale runs once a second when it should only run when aspect ratio changes...? bug
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void SetMMScale(ref ViewManager __instance, ref bool ___needsMenuPositionUpdate, ref float ___scaleFactor, ref float ___cachedScreenAspectRatio, ref float ___cachedAvatarHeight)
        {
            //correct main menu - pretty much UpdateMenuPosition()
            Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            float num = Mathf.Abs(rotationPivot.localRotation.eulerAngles.z);
            float settingsFloat = MetaPort.Instance.settings.GetSettingsFloat("GeneralMinimumMenuTilt");
            if (MetaPort.Instance.isUsingVr && (num <= settingsFloat || num >= 360f - settingsFloat))
            {
                __instance.gameObject.transform.rotation = Quaternion.LookRotation(rotationPivot.forward, Vector3.up);
            }
            else
            {
                __instance.gameObject.transform.eulerAngles = new Vector3(rotationPivot.eulerAngles.x, rotationPivot.eulerAngles.y, rotationPivot.eulerAngles.z);
            }
            __instance.gameObject.transform.position = rotationPivot.position + rotationPivot.forward * 1f * ___scaleFactor;
            ___needsMenuPositionUpdate = false;
        }
    }
}
