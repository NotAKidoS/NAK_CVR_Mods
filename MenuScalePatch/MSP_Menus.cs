using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using UnityEngine;

namespace NAK.MenuScalePatch.Helpers;

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

    //Debug/Integration
    public static bool DisableQMHelper;
    public static bool DisableQMHelper_VR;
    public static bool DisableMMHelper;
    public static bool DisableMMHelper_VR;

    internal static bool isIndependentHeadTurn = false;

    internal static void ToggleDesktopInputMethod(bool flag)
    {
        if (MetaPort.Instance.isUsingVr) return;

        ViewManager.Instance._desktopMouseMode = flag;
        CVR_MenuManager.Instance._desktopMouseMode = flag;

        RootLogic.Instance.ToggleMouse(flag);
        CVRInputManager.Instance.inputEnabled = !flag;
        CVR_MenuManager.Instance.desktopControllerRay.enabled = !flag;
    }

    internal static void HandleIndependentHeadTurnInput()
    {
        //angle of independent look axis
        bool isPressed = CVRInputManager.Instance.independentHeadTurn || CVRInputManager.Instance.independentHeadToggle;
        if (isPressed && !isIndependentHeadTurn)
        {
            isIndependentHeadTurn = true;
            MSP_MenuInfo.ToggleDesktopInputMethod(false);
            QuickMenuHelper.Instance.UpdateWorldAnchors();
            MainMenuHelper.Instance.UpdateWorldAnchors();
        }
        else if (!isPressed && isIndependentHeadTurn)
        {
            float angleX = MovementSystem.Instance._followAngleX;
            float angleY = MovementSystem.Instance._followAngleY;
            float manualAngleX = MovementSystem.Instance._manualAngleX;
            if (angleY == 0f && angleX == manualAngleX)
            {
                isIndependentHeadTurn = false;
                MSP_MenuInfo.ToggleDesktopInputMethod(true);
                QuickMenuHelper.Instance.NeedsPositionUpdate = true;
                MainMenuHelper.Instance.NeedsPositionUpdate = true;
            }
        }
    }
}