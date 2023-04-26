using UnityEngine;
using Valve.VR;

namespace NAK.TrackedControllerFix;

public class TrackedControllerFixer : MonoBehaviour
{
    public SteamVR_Input_Sources inputSource;
    public int deviceIndex;

    private SteamVR_TrackedObject trackedObject;
    private SteamVR_Behaviour_Pose oldBehaviourPose;
    private SteamVR_Action_Pose actionPose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose", false);

    private void Start()
    {
        trackedObject = gameObject.AddComponent<SteamVR_TrackedObject>();
        oldBehaviourPose = gameObject.GetComponent<SteamVR_Behaviour_Pose>();
        oldBehaviourPose.broadcastDeviceChanges = false; //this fucks us
        if (actionPose != null) CheckDeviceIndex();
    }

    private void OnEnable()
    {
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged += OnDeviceConnectedChanged;
        oldBehaviourPose.enabled = false;
    }

    private void OnDisable()
    {
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged -= OnDeviceConnectedChanged;
        oldBehaviourPose.enabled = true;
    }

    private void OnDeviceConnectedChanged(SteamVR_Action_Pose changedAction, SteamVR_Input_Sources changedSource, bool connected)
    {
        if (actionPose != changedAction) actionPose = changedAction;
        if (changedSource != inputSource) return;
        CheckDeviceIndex();
    }

    private void CheckDeviceIndex()
    {
        if (actionPose[inputSource].active && actionPose[inputSource].deviceIsConnected)
        {
            int trackedDeviceIndex = (int)actionPose[inputSource].trackedDeviceIndex;
            if (deviceIndex != trackedDeviceIndex)
            {
                deviceIndex = trackedDeviceIndex;
                trackedObject.SetDeviceIndex(deviceIndex);
            }
        }
    }
}