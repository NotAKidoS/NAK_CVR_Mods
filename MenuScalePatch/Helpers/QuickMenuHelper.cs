using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.Melons.MenuScalePatch.Helpers;

/**

    This helper is assigned to the QuickMenu object.
    The DefaultExecutionOrder attribute saves me from needing
    to use OnPreRender() callback... yay.

**/

[DefaultExecutionOrder(20000)]
public class QuickMenuHelper : MonoBehaviour
{
    public static QuickMenuHelper Instance;
    public Transform worldAnchor;
    public Transform handAnchor;
    public bool NeedsPositionUpdate;
    public bool MenuIsOpen;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (!MenuIsOpen) return;

        if (MSP_MenuInfo.PlayerAnchorMenus || NeedsPositionUpdate || MetaPort.Instance.isUsingVr)
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
        GameObject vrAnchor = new GameObject("MSP_QMVR_Anchor");
        vrAnchor.transform.parent = PlayerSetup.Instance.transform;
        vrAnchor.transform.localPosition = Vector3.zero;
        worldAnchor = vrAnchor.transform;
    }

    public void UpdateWorldAnchors(bool updateMenuPos = false)
    {
        if (worldAnchor == null || MSP_MenuInfo.CameraTransform == null) return;

        worldAnchor.rotation = MSP_MenuInfo.CameraTransform.rotation;
        worldAnchor.position = MSP_MenuInfo.CameraTransform.position;
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

    //Desktop Quick Menu
    public void HandleDesktopPosition()
    {
        if (MSP_MenuInfo.CameraTransform == null || MSP_MenuInfo.DisableQMHelper) return;

        Transform activeAnchor = MSP_MenuInfo.isIndependentHeadTurn ? worldAnchor : MSP_MenuInfo.CameraTransform;
        transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.rotation = activeAnchor.rotation;
        transform.position = activeAnchor.position + activeAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
    }

    //VR Quick Menu
    public void HandleVRPosition()
    {
        if (handAnchor == null || MSP_MenuInfo.DisableQMHelper_VR) return;

        if (MSP_MenuInfo.WorldAnchorQM)
        {
            transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
            transform.position = worldAnchor.position + worldAnchor.transform.forward * 1f * MSP_MenuInfo.ScaleFactor;
            transform.rotation = worldAnchor.rotation;
            return;
        }
        transform.localScale = new Vector3(1f * MSP_MenuInfo.ScaleFactor, 1f * MSP_MenuInfo.ScaleFactor, 1f);
        transform.position = handAnchor.position;
        transform.rotation = handAnchor.rotation;
    }
}