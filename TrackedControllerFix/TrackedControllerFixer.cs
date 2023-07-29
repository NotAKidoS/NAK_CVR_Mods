using ABI_RC.Core.Savior;
using UnityEngine;
using Valve.VR;

namespace NAK.TrackedControllerFix;

public class TrackedControllerFixer : MonoBehaviour
{
    #region Variables

    public SteamVR_Input_Sources inputSource;
    public int deviceIndex = -1;

    private SteamVR_TrackedObject _trackedObject;
    private SteamVR_Behaviour_Pose _oldBehaviourPose;
    private SteamVR_Action_Pose _actionPose;
    private SteamVR_RenderModel _renderModel;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        _trackedObject = gameObject.AddComponent<SteamVR_TrackedObject>();
        _oldBehaviourPose = gameObject.GetComponent<SteamVR_Behaviour_Pose>();
        _oldBehaviourPose.broadcastDeviceChanges = false; //this messes us up
        _renderModel = gameObject.GetComponentInChildren<SteamVR_RenderModel>();
        _actionPose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose", false);
    }

    private void OnEnable()
    {
        UpdateBehaviourPose(false);
        UpdateActionPose(true);
    }

    private void OnDisable()
    {
        UpdateBehaviourPose(true);
        UpdateActionPose(false);
    }

    private void Update()
    {
        if (_oldBehaviourPose.enabled)
            return;

        if (deviceIndex < 0)
            CheckDeviceIndex();
    }

    #endregion

    #region Private Methods

    private void UpdateBehaviourPose(bool enable)
    {
        if (CheckVR.Instance.forceOpenXr)
            return;

        if (_oldBehaviourPose == null)
            return;

        _oldBehaviourPose.enabled = enable;
    }

    private void UpdateActionPose(bool enable)
    {
        if (CheckVR.Instance.forceOpenXr)
            return;

        if (_actionPose == null)
            return;

        if (enable)
        {
            _actionPose[inputSource].onDeviceConnectedChanged += OnDeviceConnectedChanged;
            CheckDeviceIndex();
            return;
        }

        _actionPose[inputSource].onDeviceConnectedChanged -= OnDeviceConnectedChanged;
    }

    private void OnDeviceConnectedChanged(SteamVR_Action_Pose changedAction, SteamVR_Input_Sources changedSource, bool connected)
    {
        _actionPose = changedAction;
        if (changedSource != inputSource)
            return;

        CheckDeviceIndex();
    }

    private void CheckDeviceIndex()
    {
        if (!_actionPose[inputSource].deviceIsConnected)
            return;

        int trackedDeviceIndex = (int)_actionPose[inputSource].trackedDeviceIndex;
        if (deviceIndex == trackedDeviceIndex)
            return;

        deviceIndex = trackedDeviceIndex;
        _trackedObject?.SetDeviceIndex(deviceIndex);
        _renderModel?.SetDeviceIndex(deviceIndex);
    }

    #endregion
}