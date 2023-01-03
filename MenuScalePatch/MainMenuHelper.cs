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

//TODO: Implement desktop ratio scaling back to MM

/**

    This helper is assigned to the MainMenu object.
    The DefaultExecutionOrder attribute saves me from needing
    to use OnPreRender() callback... yay.

**/

[DefaultExecutionOrder(999999)]
public class MainMenuHelper : MonoBehaviour
{
    public static MainMenuHelper Instance;
    public Transform worldAnchor;

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
        GameObject vrAnchor = new GameObject("MSP_MMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.vrCameraRig.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        this.worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors()
    {
        if (this.worldAnchor == null || MSP_MenuInfo.CameraTransform == null) return;

        if (MetaPort.Instance.isUsingVr)
        {
            float zRotation = Mathf.Abs(MSP_MenuInfo.CameraTransform.localRotation.eulerAngles.z);
            float minTilt = MetaPort.Instance.settings.GetSettingsFloat("GeneralMinimumMenuTilt", 0f);
            if (zRotation <= minTilt || zRotation >= 360f - minTilt)
            {
                this.worldAnchor.rotation = Quaternion.LookRotation(MSP_MenuInfo.CameraTransform.forward, Vector3.up);
            }
            else
            {
                this.worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
            }
            this.worldAnchor.position = MSP_MenuInfo.CameraTransform.position + MSP_MenuInfo.CameraTransform.forward * 2f * MSP_MenuInfo.ScaleFactor;
        }
        else
        {
            this.worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
            this.worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
        }
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

    //Desktop Main Menu
    public void HandleDesktopPosition()
    {
        if (MSP_MenuInfo.CameraTransform == null || MSP_MenuInfo.DisableMMHelper) return;

        Transform activeAnchor = independentHeadTurn ? this.worldAnchor : MSP_MenuInfo.CameraTransform;
        this.transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor, 0.9f * MSP_MenuInfo.ScaleFactor, 1f);
        this.transform.eulerAngles = activeAnchor.eulerAngles;
        this.transform.position = activeAnchor.position + activeAnchor.forward * 1f * MSP_MenuInfo.ScaleFactor;
    }

    //VR Main Menu
    public void HandleVRPosition()
    {
        if (this.worldAnchor == null || MSP_MenuInfo.DisableMMHelper_VR) return;
        this.transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor * 1.8f, 0.9f * MSP_MenuInfo.ScaleFactor * 1.8f, 1f);
        this.transform.position = this.worldAnchor.position;
        this.transform.eulerAngles = this.worldAnchor.eulerAngles;
    }
}

