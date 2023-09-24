using ABI_RC.Systems.UI;
using NAK.DesktopVRSwitch.VRModeTrackers;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace NAK.DesktopVRSwitch;

public class VRModeSwitchManager : MonoBehaviour
{
    #region Static

    public static VRModeSwitchManager Instance { get; private set; }
    
    public static void RegisterVRModeTracker(VRModeTracker observer) => observer.TrackerInit();
    public static void UnregisterVRModeTracker(VRModeTracker observer) => observer.TrackerDestroy();
    
    #endregion

    #region Variables

    // Settings
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
    }

    #endregion

    #region Public Methods

    private static bool IsInXR() => XRGeneralSettings.Instance.Manager.activeLoader != null;
    
    public void AttemptSwitch() => StartCoroutine(StartSwitchInternal());
    
    #endregion
    
    #region Private Methods

    private IEnumerator StartSwitchInternal()
    {
        if (SwitchInProgress) 
            yield break;
        
        bool useWorldTransition = UseWorldTransition;
        SwitchInProgress = true;
        
        yield return null;
    
        if (useWorldTransition)
            yield return StartCoroutine(StartTransition());

        bool isUsingVr = IsInXR();

        InvokeOnPreSwitch(isUsingVr);

        yield return StartCoroutine(XRAndReloadAvatar(!isUsingVr));

        if (useWorldTransition)
            yield return StartCoroutine(ContinueTransition());

        SwitchInProgress = false;
    }
    
    private IEnumerator XRAndReloadAvatar(bool start)
    {
        yield return StartCoroutine(start ? XRHandler.StartXR() : XRHandler.StopXR());

        bool isUsingVr = IsInXR();
        if (isUsingVr == start)
        {
            ReloadAvatar();
            InvokeOnPostSwitch(start);
        }
        else
        {
            InvokeOnFailedSwitch(start);
        }
    }
    
    private void ReloadAvatar()
    {
        if (!ReloadLocalAvatar) 
            return;
        
        Utils.ClearLocalAvatar();
        Utils.ReloadLocalAvatar();
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

    public class VRModeEventArgs : EventArgs
    {
        public bool IsUsingVr { get; }
        public Camera PlayerCamera { get; }

        public VRModeEventArgs(bool isUsingVr, Camera playerCamera)
        {
            IsUsingVr = isUsingVr;
            PlayerCamera = playerCamera;
        }
    }
    
    public static event EventHandler<VRModeEventArgs> OnPreVRModeSwitch;
    public static event EventHandler<VRModeEventArgs> OnPostVRModeSwitch;
    public static event EventHandler<VRModeEventArgs> OnFailVRModeSwitch;

    private void InvokeOnPreSwitch(bool isUsingVr) => SafeInvokeUnityEvent(OnPreVRModeSwitch, isUsingVr);
    private void InvokeOnPostSwitch(bool isUsingVr) => SafeInvokeUnityEvent(OnPostVRModeSwitch, isUsingVr);
    private void InvokeOnFailedSwitch(bool isUsingVr) => SafeInvokeUnityEvent(OnFailVRModeSwitch, isUsingVr);

    private void SafeInvokeUnityEvent(EventHandler<VRModeEventArgs> switchEvent, bool isUsingVr)
    {
        try
        {
            Camera playerCamera = Utils.GetPlayerCameraObject(isUsingVr).GetComponent<Camera>();
            switchEvent?.Invoke(this, new VRModeEventArgs(isUsingVr, playerCamera));
        }
        catch (Exception e)
        {
            DesktopVRSwitch.Logger.Error($"Error in event handler: {e}");
        }
    }
    
    #endregion
}