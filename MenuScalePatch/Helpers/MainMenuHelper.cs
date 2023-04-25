using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using UnityEngine;


namespace NAK.MenuScalePatch.Helpers;

//TODO: Implement desktop ratio scaling back to MM

/**

    This helper is assigned to the MainMenu object.
    The DefaultExecutionOrder attribute saves me from needing
    to use OnPreRender() callback... yay.

**/

[DefaultExecutionOrder(20000)]
public class MainMenuHelper : MonoBehaviour
{
    public static MainMenuHelper Instance;
    public Transform worldAnchor;
    public bool NeedsPositionUpdate;
    public bool MenuIsOpen;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (!MenuIsOpen) return;

        if (MSP_MenuInfo.PlayerAnchorMenus || NeedsPositionUpdate)
        {
            UpdateMenuPosition();
        }

        if (MSP_MenuInfo.UseIndependentHeadTurn)
        {
            MSP_MenuInfo.HandleIndependentHeadTurnInput();
        }
    }

    public void CreateWorldAnchors()
    {
        //VR specific anchor
        GameObject vrAnchor = new GameObject("MSP_MMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.vrCameraRig.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors(bool updateMenuPos = false)
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
                worldAnchor.rotation = MSP_MenuInfo.CameraTransform.rotation;
            }
            worldAnchor.position = MSP_MenuInfo.CameraTransform.position + MSP_MenuInfo.CameraTransform.forward * 2f * MSP_MenuInfo.ScaleFactor;
        }
        else
        {
            worldAnchor.rotation = MSP_MenuInfo.CameraTransform.rotation;
            worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
        }
        if (updateMenuPos) UpdateMenuPosition();
    }

    public void UpdateMenuPosition()
    {
        NeedsPositionUpdate = false;
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

        Transform activeAnchor = MSP_MenuInfo.isIndependentHeadTurn ? worldAnchor : MSP_MenuInfo.CameraTransform;
        transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor, 0.9f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.position = activeAnchor.position + activeAnchor.forward * 1f * MSP_MenuInfo.ScaleFactor * MSP_MenuInfo.AspectRatio;
        transform.rotation = activeAnchor.rotation;
    }

    //VR Main Menu
    public void HandleVRPosition()
    {
        if (worldAnchor == null || MSP_MenuInfo.DisableMMHelper_VR) return;

        transform.localScale = new Vector3(1.6f * MSP_MenuInfo.ScaleFactor * 1.8f, 0.9f * MSP_MenuInfo.ScaleFactor * 1.8f, 1f);
        transform.position = worldAnchor.position;
        transform.rotation = worldAnchor.rotation;
    }
}
