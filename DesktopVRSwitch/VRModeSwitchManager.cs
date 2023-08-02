using ABI_RC.Systems.UI;
using NAK.DesktopVRSwitch.VRModeTrackers;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Valve.VR;

/**

    I am unsure about this observer approach as only a few things need OnPre and OnFailed switch.
    Those wouldn't be needed if I can start OpenVR before all that anyways.

    Or... I just start OpenVR and see if it worked. OnPreSwitch would only be needed by menus & transition.

    I think I should just use Unity Events as they would allow easier mod support. Subscribe to what you need.

**/

namespace NAK.DesktopVRSwitch;

public class VRModeSwitchManager : MonoBehaviour
{
    public static VRModeSwitchManager Instance { get; private set; }

    // I don't think I *need* this. Only using cause I don't want stuff just existing.
    private static readonly List<VRModeTracker> _vrModeTrackers = new List<VRModeTracker>();

    public static event UnityAction<bool> OnPreVRModeSwitch;
    public static event UnityAction<bool> OnPostVRModeSwitch;
    public static event UnityAction<bool> OnFailVRModeSwitch;
    private const string XRSETTINGS_DEVICE = "OpenVR";

    public static void RegisterVRModeTracker(VRModeTracker observer)
    {
        _vrModeTrackers.Add(observer);
        observer.TrackerInit();
    }

    public static void UnregisterVRModeTracker(VRModeTracker observer)
    {
        _vrModeTrackers.Remove(observer);
        observer.TrackerDestroy();
    }

    // Settings
    public bool UseWorldTransition = true;
    public bool ReloadLocalAvatar = true;
    
    public bool SwitchInProgress { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }
        Instance = this;
    }

    public void AttemptSwitch()
    {
        StartCoroutine(StartSwitchCoroutine());
    }

    private IEnumerator StartSwitchCoroutine()
    {
        if (SwitchInProgress)
        {
            yield break;
        }
        SwitchInProgress = true;
        yield return null;


        if (UseWorldTransition)
        {   // start visual transition and wait for it to complete
            WorldTransitionSystem.Instance.StartTransition();
            yield return new WaitForSeconds(WorldTransitionSystem.Instance.CurrentInLength);
        }

        // Check if OpenVR is running
        bool isUsingVr = IsInXR();

        InvokeOnPreSwitch(isUsingVr);

        // Start switch
        if (!isUsingVr)
            yield return StartCoroutine(StartXR());
        else
            StopXR();

        // Check for updated VR mode
        if (isUsingVr != IsInXR())
        {
            // reload the local avatar
            if (ReloadLocalAvatar)
            {
                Utils.ClearLocalAvatar();
                Utils.ReloadLocalAvatar();
            }

            InvokeOnPostSwitch(!isUsingVr);
        }
        else
        {
            InvokeOnFailedSwitch(!isUsingVr);
        }

        if (UseWorldTransition)
        {   // would be cool to have out length
            WorldTransitionSystem.Instance.ContinueTransition();
            yield return new WaitForSeconds(WorldTransitionSystem.Instance.CurrentInLength);
        }

        SwitchInProgress = false;
        yield break;
    }

    private void SafeInvokeUnityEvent(UnityAction<bool> switchEvent, bool isUsingVr)
    {
        try
        {
            switchEvent.Invoke(isUsingVr);
        }
        catch (Exception e)
        {
            Debug.Log($"Error in event handler: {e}");
        }
    }

    private void InvokeOnPreSwitch(bool isUsingVr)
    {
        SafeInvokeUnityEvent(OnPreVRModeSwitch, isUsingVr);
    }

    private void InvokeOnPostSwitch(bool isUsingVr)
    {
        SafeInvokeUnityEvent(OnPostVRModeSwitch, isUsingVr);
    }

    private void InvokeOnFailedSwitch(bool isUsingVr)
    {
        SafeInvokeUnityEvent(OnFailVRModeSwitch, isUsingVr);
    }

    public bool IsInXR() => XRGeneralSettings.Instance.Manager.activeLoader != null;

    private IEnumerator StartXR()
    {
        yield return null; // wait a frame before checking
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        yield return null;
        yield break;
    }

    private void StopXR()
    {
        if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            return;
        
        // Forces SteamVR to reinitialize SteamVR_Input next switch
        SteamVR_ActionSet_Manager.DisableAllActionSets();
        SteamVR_Input.initialized = false;

        // Remove SteamVR behaviour & render
        DestroyImmediate(SteamVR_Behaviour.instance.gameObject);
        SteamVR.enabled = false; // disposes SteamVR

        // Disable UnityXR
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();

        // We don't really need to wait a frame on Stop()
    }
}