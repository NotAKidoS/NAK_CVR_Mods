using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using HarmonyLib;
using Valve.VR;

namespace NAK.GestureLock.HarmonyPatches;

class Patches
{
    private static bool isLocked;
    private static float oldGestureLeft;
    private static float oldGestureRight;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputModuleSteamVR), nameof(InputModuleSteamVR.UpdateInput))]
    private static void Postfix_InputModuleSteamVR_UpdateInput
    (
        ref CVRInputManager ____inputManager,
        ref VRTrackerManager ____trackerManager,
        ref SteamVR_Action_Boolean ___steamVrIndexGestureToggle
    )
    {
        if (!MetaPort.Instance.isUsingVr)
        {
            return;
        }

        if (___steamVrIndexGestureToggle.stateDown && !____trackerManager.trackerNames.Contains("knuckles"))
        {
            isLocked = !isLocked;
            oldGestureLeft = ____inputManager.gestureLeft;
            oldGestureRight = ____inputManager.gestureRight;
            CohtmlHud.Instance.ViewDropTextImmediate("", "Gesture Lock", "Gestures " + (isLocked ? "Locked" : "Unlocked"));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputManager), nameof(CVRInputManager.Update))]
    private static void Postfix_CVRInputManager_Update
    (
        ref float ___gestureLeft,
        ref float ___gestureRight
    )
    {
        if (!MetaPort.Instance.isUsingVr)
        {
            return;
        }

        if (isLocked)
        {
            // Dont override raw, other systems like the camera gesture recognizer need it.
            ___gestureLeft = oldGestureLeft;
            ___gestureRight = oldGestureRight;
        }
    }
}