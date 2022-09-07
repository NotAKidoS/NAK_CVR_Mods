using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using HarmonyLib;
using MelonLoader;
using Valve.VR;

namespace GestureLock;

public class GestureLock : MelonMod
{
    [HarmonyPatch]
    private class HarmonyPatches
    {
        private static bool isLocked = false;
        private static bool toggleLock = false;
        private static float oldGestureLeft = 0;
        private static float oldGestureRight = 0;

        //Read VR Buttons
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InputModuleSteamVR), "UpdateInput")]
        private static void AfterUpdateInput(ref SteamVR_Action_Boolean ___steamVrIndexGestureToggle, ref VRTrackerManager ____trackerManager)
        {
            if (!MetaPort.Instance.isUsingVr) return;

            toggleLock = false;
            if (___steamVrIndexGestureToggle.stateDown)
            {
                if (!____trackerManager.trackerNames.Contains("knuckles"))
                {
                    toggleLock = true;
                }
            }
        }

        //Apply GestureLock
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRInputManager), "Update")]
        private static void AfterUpdate(ref float ___gestureLeftRaw, ref float ___gestureLeft, ref float ___gestureRightRaw, ref float ___gestureRight)
        {
            if (!MetaPort.Instance.isUsingVr) return;

            if (toggleLock)
            {
                isLocked = !isLocked;
                oldGestureLeft = ___gestureLeft;
                oldGestureRight = ___gestureRight;
                MelonLogger.Msg("Gestures " + (isLocked ? "Locked" : "Unlocked"));
                CohtmlHud.Instance.ViewDropTextImmediate("", "Gesture Lock ", "Gestures " + (isLocked ? "Locked" : "Unlocked"));
            }
            if (isLocked)
            {
                ___gestureLeftRaw = oldGestureLeft;
                ___gestureLeft = oldGestureLeft;
                ___gestureRightRaw = oldGestureRight;
                ___gestureRight = oldGestureRight;
            }
        }
    }
}
