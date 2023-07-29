using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.InputManagement.InputModules;
using ABI_RC.Systems.InputManagement.XR;
using HarmonyLib;
using Valve.VR;

namespace NAK.GestureLock.HarmonyPatches;

internal class CVRInputModule_XRPatches
{
    // Get input from SteamVR because new input system is nerfed for OpenXR...
    private static readonly SteamVR_Action_Boolean _gestureToggleButton = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ControllerToggleGestures", false);
    
    private static bool _isLocked;
    private static float _oldGestureLeft;
    private static float _oldGestureRight;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRInputModule_XR), nameof(CVRInputModule_XR.Update_Emotes))]
    private static void Postfix_CVRInputModule_XR_Update_Emotes(ref CVRInputModule_XR __instance)
    {
        if (!MetaPort.Instance.isUsingVr) 
            return;

        bool leftInput = _gestureToggleButton.GetLastStateDown(SteamVR_Input_Sources.LeftHand);
        bool rightInput = _gestureToggleButton.GetLastStateDown(SteamVR_Input_Sources.RightHand);

        if (leftInput && __instance._leftModule.Type == EXRControllerType.Index || __instance._inputManager.oneHanded)
            return;

        if (rightInput && __instance._rightModule.Type == EXRControllerType.Index || __instance._inputManager.oneHanded)
            return;
        
        if (leftInput || rightInput)
        {
            _isLocked = !_isLocked;
            _oldGestureLeft = __instance._inputManager.gestureLeft;
            _oldGestureRight = __instance._inputManager.gestureRight;
            CohtmlHud.Instance.ViewDropTextImmediate("", "Gesture Lock", "Gestures " + (_isLocked ? "Locked" : "Unlocked"));
        }

        if (!_isLocked)
            return;
        
        __instance._inputManager.gestureLeft = _oldGestureLeft;
        __instance._inputManager.gestureRight = _oldGestureRight;
    }
}