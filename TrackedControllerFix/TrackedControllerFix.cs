using UnityEngine;
using Valve.VR;

namespace NAK.TrackedControllerFix;

public class TrackedControllerFixer : MonoBehaviour
{
    public SteamVR_Input_Sources inputSource;
    public int deviceIndex = -1;

    SteamVR_TrackedObject trackedObject;
    SteamVR_Behaviour_Pose oldBehaviourPose;
    SteamVR_Action_Pose actionPose;
    
    SteamVR_RenderModel renderModel;

    public void Initialize()
    {
        trackedObject = gameObject.AddComponent<SteamVR_TrackedObject>();
        oldBehaviourPose = gameObject.GetComponent<SteamVR_Behaviour_Pose>();
        oldBehaviourPose.broadcastDeviceChanges = false; //this fucks us
        oldBehaviourPose.enabled = false;

        renderModel = gameObject.GetComponentInChildren<SteamVR_RenderModel>();

        actionPose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose", false);
        if (actionPose != null) CheckDeviceIndex();
    }

    void OnEnable()
    {
        // DesktopVRSwitch support
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged += OnDeviceConnectedChanged;
        if (oldBehaviourPose != null)
            oldBehaviourPose.enabled = false;
    }

    void OnDisable()
    {
        // DesktopVRSwitch support
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged -= OnDeviceConnectedChanged;
        if (oldBehaviourPose != null)
            oldBehaviourPose.enabled = true;
    }

    void Update()
    {
        if (deviceIndex < 0)
        {
            CheckDeviceIndex();
        }
    }

    void OnDeviceConnectedChanged(SteamVR_Action_Pose changedAction, SteamVR_Input_Sources changedSource, bool connected)
    {
        if (actionPose != changedAction) actionPose = changedAction;
        if (changedSource != inputSource) return;
        CheckDeviceIndex();
    }

    void CheckDeviceIndex()
    {
        if (actionPose[inputSource].deviceIsConnected)
        {
            int trackedDeviceIndex = (int)actionPose[inputSource].trackedDeviceIndex;
            if (deviceIndex != trackedDeviceIndex)
            {
                deviceIndex = trackedDeviceIndex;
                trackedObject?.SetDeviceIndex(deviceIndex);
                renderModel?.SetDeviceIndex(deviceIndex);
            }
        }
    }
}