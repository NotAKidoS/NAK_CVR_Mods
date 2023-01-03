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

namespace NAK.Melons.MenuScalePatch.Helpers;

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

    void OnDisable()
    {
        independentHeadTurn = false;
        returnIndependentHeadTurn = false;
        prevIndependentHeadTurn = false;
    }

    public void ToggleDesktopInputMethod(bool flag)
    {
        PlayerSetup.Instance._movementSystem.disableCameraControl = flag;
        CVRInputManager.Instance.inputEnabled = !flag;
        RootLogic.Instance.ToggleMouse(flag);
        CVR_MenuManager.Instance.desktopControllerRay.enabled = !flag;
        Traverse.Create(CVR_MenuManager.Instance).Field("_desktopMouseMode").SetValue(flag);
    }

    public void CreateWorldAnchors()
    {
        //VR specific anchor
        GameObject vrAnchor = new GameObject("MSP_QMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.vrCameraRig.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        this.worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors()
    {
        if (this.worldAnchor == null || MSP_MenuInfo.CameraTransform == null) return;

        this.worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
        this.worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
    }

    public void UpdateMenuPosition()
    {
        if (MetaPort.Instance.isUsingVr)
        {
            HandleVRPosition();
            return;
        }

        bool independentHeadTurnChanged = CVRInputManager.Instance.independentHeadTurn != prevIndependentHeadTurn;
        if (independentHeadTurnChanged)
        {
            prevIndependentHeadTurn = CVRInputManager.Instance.independentHeadTurn;
            //if pressing but not already enabled
            if (prevIndependentHeadTurn)
            {
                if (!independentHeadTurn)
                {
                    UpdateWorldAnchors();
                    ToggleDesktopInputMethod(!prevIndependentHeadTurn);
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
            float angle = (float)ms_followAngleY.GetValue(MovementSystem.Instance);
            if (angle == 0f)
            {
                independentHeadTurn = false;
                returnIndependentHeadTurn = false;
                ToggleDesktopInputMethod(!prevIndependentHeadTurn);
            }
        }

        HandleDesktopPosition();
    }

    //Desktop Quick Menu
    public void HandleDesktopPosition()
    {
        if (MSP_MenuInfo.CameraTransform == null || MSP_MenuInfo.DisableQMHelper) return;

        Transform activeAnchor = independentHeadTurn ? this.worldAnchor : MSP_MenuInfo.CameraTransform;
        this.transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        this.transform.eulerAngles = activeAnchor.eulerAngles;
        this.transform.position = activeAnchor.position + activeAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
    }

    //VR Quick Menu
    public void HandleVRPosition()
    {
        if (this.handAnchor == null || MSP_MenuInfo.DisableQMHelper_VR) return;

        if (MSP_MenuInfo.WorldAnchorQM)
        {
            this.transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
            this.transform.eulerAngles = this.worldAnchor.eulerAngles;
            this.transform.position = this.worldAnchor.position + this.worldAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
            return;
        }
        this.transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        this.transform.position = this.handAnchor.position;
        this.transform.rotation = this.handAnchor.rotation;
    }
}