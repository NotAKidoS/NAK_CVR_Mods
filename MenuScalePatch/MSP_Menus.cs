using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.MenuScalePatch.Helpers;

public class MSP_MenuInfo
{
    //Shared Info
    internal static float ScaleFactor = 1f;
    internal static float AspectRatio = 1f;
    internal static Transform CameraTransform;

    //Settings...?
    internal static bool WorldAnchorQM = false;
    internal static bool UseIndependentHeadTurn = true;
    internal static bool PlayerAnchorMenus = true;

    //if other mods need to disable?
    internal static bool DisableQMHelper;
    internal static bool DisableQMHelper_VR;
    internal static bool DisableMMHelper;
    internal static bool DisableMMHelper_VR;

    //reflection (traverse sucks ass)
    private static readonly FieldInfo _desktopMouseModeQM = typeof(ViewManager).GetField("_desktopMouseMode", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _desktopMouseModeMM = typeof(CVR_MenuManager).GetField("_desktopMouseMode", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static void ToggleDesktopInputMethod(bool flag)
    {
        if (MetaPort.Instance.isUsingVr) return;

        _desktopMouseModeQM.SetValue(ViewManager.Instance, flag);
        _desktopMouseModeMM.SetValue(CVR_MenuManager.Instance, flag);
        
        RootLogic.Instance.ToggleMouse(flag);
        CVRInputManager.Instance.inputEnabled = !flag;
        PlayerSetup.Instance._movementSystem.disableCameraControl = flag;
        CVR_MenuManager.Instance.desktopControllerRay.enabled = !flag;
    }

    internal static readonly FieldInfo ms_followAngleY = typeof(MovementSystem).GetField("_followAngleY", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static bool independentHeadTurn = false;

    internal static void HandleIndependentLookInput()
    {
        //angle of independent look axis
        bool isPressed = CVRInputManager.Instance.independentHeadTurn || CVRInputManager.Instance.independentHeadToggle;
        if (isPressed && !independentHeadTurn)
        {
            independentHeadTurn = true;
            MSP_MenuInfo.ToggleDesktopInputMethod(false);
            QuickMenuHelper.Instance.UpdateWorldAnchors();
            MainMenuHelper.Instance.UpdateWorldAnchors();
        }
        else if (!isPressed && independentHeadTurn)
        {
            float angle = (float)ms_followAngleY.GetValue(MovementSystem.Instance);
            if (angle == 0f)
            {
                independentHeadTurn = false;
                MSP_MenuInfo.ToggleDesktopInputMethod(true);
            }
        }
    }
}