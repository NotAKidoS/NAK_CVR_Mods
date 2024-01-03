using ABI_RC.Systems.UI;
using System.Collections;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.VRModeSwitch;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;

namespace NAK.DesktopVRSwitch;

 public class VRModeSwitchManager : MonoBehaviour
{
    public static VRModeSwitchManager Instance { get; private set; }

    #region Variables

    // Settings
    public bool DesktopVRSwitchEnabled;
    public bool UseWorldTransition = true;
    public bool ReloadLocalAvatar = true;

    public bool SwitchInProgress { get; private set; }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;

        DesktopVRSwitchEnabled = MetaPort.Instance.settings.GetSettingsBool("ExperimentalDesktopVRSwitch");
        MetaPort.Instance.settings.settingBoolChanged.AddListener(OnSettingsBoolChanged);
    }

    private void Update()
    {
        if (!DesktopVRSwitchEnabled)
            return;
        
        if (SwitchInProgress)
            return;

        if (CVRInputManager.Instance.switchMode)
            AttemptSwitch();
    }

    #endregion

    #region Public Methods

    public void AttemptSwitch()
    {
        if (SwitchInProgress)
            return;

        // dont allow switching during world transfer, itll explode violently
        if (CVRObjectLoader.Instance.IsLoadingWorldToJoin())
            return;
        
        StartCoroutine(StartSwitchInternal());
    }

    #endregion

    #region Private Methods
    
    private void OnSettingsBoolChanged(string settingName, bool val)
    {
        if (settingName == "ExperimentalDesktopVRSwitch")
            DesktopVRSwitchEnabled = val;
    }

    private IEnumerator StartSwitchInternal()
    {
        if (SwitchInProgress)
            yield break;

        NotifyOnPreSwitch();

        bool useWorldTransition = UseWorldTransition;
        SwitchInProgress = true;

        yield return null;

        if (useWorldTransition)
            yield return StartCoroutine(StartTransition());

        var wasInXr = IsInXR();

        InvokeOnPreSwitch(!wasInXr);

        // Note: this assumes that wasInXr has been correctly set earlier in your method.
        Task xrTask = wasInXr ? XRHandler.StopXR() : XRHandler.StartXR();

        // Wait for the task to complete. This makes the coroutine wait here until the above thread is done.
        yield return new WaitUntil(() => xrTask.IsCompleted || xrTask.IsFaulted);

        // Check task status, handle any fault that occurred during the execution of the task.
        if (xrTask.IsFaulted)
        {
            // Log and/or handle exceptions that occurred within the task.
            Exception innerException = xrTask.Exception.InnerException; // The Exception that caused the Task to enter the faulted state
            MelonLoader.MelonLogger.Error("Encountered an error while executing the XR task: " + innerException.Message);
            // Handle the exception appropriately.
        }

        if (wasInXr != IsInXR())
        {
            ReloadAvatar();
            InvokeOnPostSwitch(!wasInXr);
        }
        else
        {
            NotifyOnFailedSwitch();
            InvokeOnFailedSwitch(!wasInXr);
        }

        if (useWorldTransition)
            yield return StartCoroutine(ContinueTransition());

        SwitchInProgress = false;
    }

    private void ReloadAvatar()
    {
        if (!ReloadLocalAvatar)
            return;

        // TODO: Is there a better way to reload only locally?
        PlayerSetup.Instance.ClearAvatar();
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);
    }

    private bool IsInXR()
    {
        return XRGeneralSettings.Instance.Manager.activeLoader != null;
    }

    private UnityEngine.Camera GetPlayerCamera(bool isVr)
    {
        return (isVr ? PlayerSetup.Instance.vrCamera : PlayerSetup.Instance.desktopCamera)
            .GetComponent<UnityEngine.Camera>();
    }
    
    #endregion

    #region Transition Coroutines

    private IEnumerator StartTransition()
    {
        if (WorldTransitionSystem.Instance == null) yield break;
        WorldTransitionSystem.Instance.StartTransition();
        yield return new WaitForSeconds(WorldTransitionSystem.Instance.CurrentInLength + 0.25f);
    }

    private IEnumerator ContinueTransition()
    {
        if (WorldTransitionSystem.Instance == null) yield break;
        WorldTransitionSystem.Instance.ContinueTransition();
        yield return new WaitForSeconds(WorldTransitionSystem.Instance.CurrentInLength + 0.25f);
    }

    #endregion

    #region Event Handling

    private void InvokeOnPreSwitch(bool isUsingVr)
    {
        UnityEngine.Camera playerCamera = GetPlayerCamera(isUsingVr);

        ABI_RC.Systems.VRModeSwitch.VRModeSwitchManager.OnPreSwitchInternal?.Invoke(isUsingVr, playerCamera);
        CVRGameEventSystem.VRModeSwitch.OnPreSwitch?.Invoke(isUsingVr, playerCamera);
    }

    private void InvokeOnPostSwitch(bool isUsingVr)
    {
        UnityEngine.Camera playerCamera = GetPlayerCamera(isUsingVr);

        ABI_RC.Systems.VRModeSwitch.VRModeSwitchManager.OnPostSwitchInternal?.Invoke(isUsingVr, playerCamera);
        CVRGameEventSystem.VRModeSwitch.OnPostSwitch?.Invoke(isUsingVr, playerCamera);
    }

    private void InvokeOnFailedSwitch(bool isUsingVr)
    {
        UnityEngine.Camera playerCamera = GetPlayerCamera(isUsingVr);

        ABI_RC.Systems.VRModeSwitch.VRModeSwitchManager.OnFailedSwitchInternal?.Invoke(isUsingVr, playerCamera);
        CVRGameEventSystem.VRModeSwitch.OnFailedSwitch?.Invoke(isUsingVr, playerCamera);
    }

    #endregion

    #region Notifications

    private void NotifyOnPreSwitch()
    {
        CohtmlHud.Instance.ViewDropTextImmediate("(Local) Client",
            "VR Mode Switch", "Switching to " + (IsInXR() ? "Desktop" : "VR") + " Mode");
    }

    private void NotifyOnFailedSwitch()
    {
        // TODO: Can we get reason it failed?
        CohtmlHud.Instance.ViewDropTextImmediate("(Local) Client",
            "VR Mode Switch", "Switch failed");
    }

    #endregion
}
