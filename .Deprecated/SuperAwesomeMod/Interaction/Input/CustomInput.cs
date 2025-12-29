using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NAK.SuperAwesomeMod.Interaction;

[DefaultExecutionOrder(1000)]
public class CustomBaseInput : BaseInput
{
    private Vector2 mousePositionCache;
    
    #region Input Overrides

    public override Vector2 mousePosition => Input.mousePosition;

    public override bool GetMouseButton(int button)
        => button == (int)CVRHand.Right 
            ? CVRInputManager.Instance.interactLeftValue > 0.75f
            : CVRInputManager.Instance.interactRightValue > 0.75f;

    public override bool GetMouseButtonDown(int button)
        => button == (int)CVRHand.Right 
            ? CVRInputManager.Instance.interactLeftDown
            : CVRInputManager.Instance.interactRightDown;
    
    public override Vector2 mouseScrollDelta => Vector2.zero;
    
    public override float GetAxisRaw(string axisName)
    {
        return axisName switch
        {
            "Mouse ScrollWheel" => CVRInputManager.Instance.scrollValue,
            "Horizontal" => CVRInputManager.Instance.movementVector.x,
            "Vertical" => CVRInputManager.Instance.movementVector.y,
            _ => 0f
        };
    }

    public override bool GetButtonDown(string buttonName)
    {
        return buttonName switch
        {
            "Mouse ScrollWheel" => CVRInputManager.Instance.scrollValue > 0.1f,
            "Horizontal" => CVRInputManager.Instance.movementVector.x > 0.5f,
            "Vertical" => CVRInputManager.Instance.movementVector.y > 0.5f,
            _ => false
        };
    }

    #endregion Input Overrides

    private CVRHand lastInteractHand;
    
    private void Update()
    {
        if (!MetaPort.Instance.isUsingVr)
        {
            mousePositionCache = Input.mousePosition;
            return;
        }
        
        ControllerRay leftRay = PlayerSetup.Instance.vrRayLeft;
        ControllerRay rightRay = PlayerSetup.Instance.vrRayRight;

        if (leftRay._interactDown) lastInteractHand = leftRay.hand;
        if (rightRay._interactDown) lastInteractHand = rightRay.hand;

        Camera vrCamera = PlayerSetup.Instance.vrCam;

        // transform the raycast position to screen position
        Vector3 hitPoint = lastInteractHand == CVRHand.Left
            ? leftRay.HitPoint
            : rightRay.HitPoint;

        Vector3 screenPoint = vrCamera.WorldToScreenPoint(hitPoint);
        screenPoint.x = Mathf.Clamp(screenPoint.x, 0, Screen.width);
        screenPoint.y = Mathf.Clamp(screenPoint.y, 0, Screen.height);
        mousePositionCache = new Vector2(screenPoint.x, screenPoint.y);
    }
}

