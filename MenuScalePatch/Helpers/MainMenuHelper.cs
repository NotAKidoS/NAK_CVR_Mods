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

    void Start()
    {
        Instance = this;
        CreateWorldAnchors();
    }

    void LateUpdate()
    {
        MSP_MenuInfo.HandleIndependentLookInput();
        UpdateMenuPosition();
    }

    public void CreateWorldAnchors()
    {
        //VR specific anchor
        GameObject vrAnchor = new GameObject("MSP_MMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.vrCameraRig.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors()
    {
        if (worldAnchor == null || MSP_MenuInfo.CameraTransform == null) return;

        if (MetaPort.Instance.isUsingVr)
        {
            float zRotation = Mathf.Abs(MSP_MenuInfo.CameraTransform.localRotation.eulerAngles.z);
            float minTilt = MetaPort.Instance.settings.GetSettingsFloat("GeneralMinimumMenuTilt", 0f);
            if (zRotation <= minTilt || zRotation >= 360f - minTilt)
            {
                worldAnchor.rotation = Quaternion.LookRotation(MSP_MenuInfo.CameraTransform.forward, Vector3.up);
            }
            else
            {
                worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
            }
            worldAnchor.position = MSP_MenuInfo.CameraTransform.position + MSP_MenuInfo.CameraTransform.forward * 2f * MSP_MenuInfo.ScaleFactor;
        }
        else
        {
            worldAnchor.eulerAngles = MSP_MenuInfo.CameraTransform.eulerAngles;
            worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
        }
    }

    public void UpdateMenuPosition()
    {
        if (MetaPort.Instance.isUsingVr)
        {
            HandleVRPosition();
            return;
        }
        HandleDesktopPosition();
    }

    //Desktop Main Menu
    public void HandleDesktopPosition()
    {
        if (MSP_MenuInfo.CameraTransform == null || MSP_MenuInfo.DisableMMHelper) return;
        Transform activeAnchor = MSP_MenuInfo.independentHeadTurn ? worldAnchor : MSP_MenuInfo.CameraTransform;
        transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor, 0.9f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.eulerAngles = activeAnchor.eulerAngles;
        transform.position = activeAnchor.position + activeAnchor.forward * 1f * MSP_MenuInfo.ScaleFactor * MSP_MenuInfo.AspectRatio;
    }

    //VR Main Menu
    public void HandleVRPosition()
    {
        if (worldAnchor == null || MSP_MenuInfo.DisableMMHelper_VR) return;
        transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor * 1.8f, 0.9f * MSP_MenuInfo.ScaleFactor * 1.8f, 1f);
        transform.position = worldAnchor.position;
        transform.eulerAngles = worldAnchor.eulerAngles;
    }
}