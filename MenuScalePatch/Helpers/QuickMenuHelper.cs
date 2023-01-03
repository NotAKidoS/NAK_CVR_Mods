using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.MovementSystem;
using cohtml;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System.Reflection;
using NAK.Melons.MenuScalePatch.Helpers;

namespace MenuScalePatch.Helpers;

/**

    This helper is assigned to the QuickMenu object.
    The DefaultExecutionOrder attribute saves me from needing
    to use OnPreRender() callback... yay.

**/

[DefaultExecutionOrder(999999)]
public class QuickMenuHelper : MonoBehaviour
{
    public static QuickMenuHelper Instance;
    public Transform worldAnchor;
    public Transform handAnchor;

    static readonly FieldInfo ms_followAngleY = typeof(MovementSystem).GetField("_followAngleY", BindingFlags.NonPublic | BindingFlags.Instance);
    private bool independentHeadTurn = false;
    private bool returnIndependentHeadTurn = false;
    private bool prevIndependentHeadTurn = false;

    void Start()
    {
        Instance = this;
        CreateWorldAnchors();
    }

    void LateUpdate()
    {
        UpdateMenuPosition();
    }

    void OnEnable()
    {
        independentHeadTurn = false;
        returnIndependentHeadTurn = false;
        prevIndependentHeadTurn = false;
    }

    void OnDisable()
    {
        independentHeadTurn = false;
        returnIndependentHeadTurn = false;
        prevIndependentHeadTurn = false;
    }

    public void CreateWorldAnchors()
    {
        //VR specific anchor
        GameObject vrAnchor = new GameObject("MSP_QMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.vrCameraRig.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors()
    {
        if (worldAnchor == null || MSP_MenuInfo.CameraTransform == null) return;

        worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
        worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
    }

    public void UpdateMenuPosition()
    {
        if (MetaPort.Instance.isUsingVr)
        {
            HandleVRPosition();
            return;
        }

        float angle = (float)ms_followAngleY.GetValue(MovementSystem.Instance);
        bool independentHeadTurnChanged = CVRInputManager.Instance.independentHeadTurn != prevIndependentHeadTurn;
        if (independentHeadTurnChanged)
        {
            prevIndependentHeadTurn = CVRInputManager.Instance.independentHeadTurn;
            //if pressing but not already enabled
            if (prevIndependentHeadTurn)
            {
                if (!independentHeadTurn && angle == 0f)
                {
                    UpdateWorldAnchors();
                    MSP_MenuInfo.ToggleDesktopInputMethod(!prevIndependentHeadTurn);
                    independentHeadTurn = true;
                }
                returnIndependentHeadTurn = false;
            }
            else
            {
                returnIndependentHeadTurn = true;
            }
        }

        if (returnIndependentHeadTurn)
        {
            if (angle == 0f)
            {
                independentHeadTurn = false;
                returnIndependentHeadTurn = false;
                MSP_MenuInfo.ToggleDesktopInputMethod(!prevIndependentHeadTurn);
            }
        }

        HandleDesktopPosition();
    }

    //Desktop Quick Menu
    public void HandleDesktopPosition()
    {
        if (MSP_MenuInfo.CameraTransform == null || MSP_MenuInfo.DisableQMHelper) return;

        Transform activeAnchor = independentHeadTurn ? worldAnchor : MSP_MenuInfo.CameraTransform;
        transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.eulerAngles = activeAnchor.eulerAngles;
        transform.position = activeAnchor.position + activeAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
    }

    //VR Quick Menu
    public void HandleVRPosition()
    {
        if (handAnchor == null || MSP_MenuInfo.DisableQMHelper_VR) return;

        if (MSP_MenuInfo.WorldAnchorQM)
        {
            transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
            transform.eulerAngles = worldAnchor.eulerAngles;
            transform.position = worldAnchor.position + worldAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
            return;
        }
        transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.position = handAnchor.position;
        transform.rotation = handAnchor.rotation;
    }
}