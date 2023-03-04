using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
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

    //Debug/Integration
    public static bool DisableQMHelper;
    public static bool DisableQMHelper_VR;
    public static bool DisableMMHelper;
    public static bool DisableMMHelper_VR;

    //reflection
    internal static readonly FieldInfo _desktopMouseModeQM = typeof(ViewManager).GetField("_desktopMouseMode", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo _desktopMouseModeMM = typeof(CVR_MenuManager).GetField("_desktopMouseMode", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo ms_followAngleX = typeof(MovementSystem).GetField("_followAngleX", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo ms_followAngleY = typeof(MovementSystem).GetField("_followAngleY", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo ms_manualAngleX = typeof(MovementSystem).GetField("_manualAngleX", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static bool isIndependentHeadTurn = false;

    internal static void ToggleDesktopInputMethod(bool flag)
    {
        if (MetaPort.Instance.isUsingVr) return;

        _desktopMouseModeQM.SetValue(ViewManager.Instance, flag);
        _desktopMouseModeMM.SetValue(CVR_MenuManager.Instance, flag);

        RootLogic.Instance.ToggleMouse(flag);
        CVRInputManager.Instance.inputEnabled = !flag;
        CVR_MenuManager.Instance.desktopControllerRay.enabled = !flag;
    }

    internal static void HandleIndependentLookInput()
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
            float angleX = (float)ms_followAngleX.GetValue(MovementSystem.Instance);
            float angleY = (float)ms_followAngleY.GetValue(MovementSystem.Instance);
            float manualAngleX = (float)ms_manualAngleX.GetValue(MovementSystem.Instance);
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