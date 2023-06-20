using ABI_RC.Systems.UI;
using NAK.DesktopVRSwitch.VRModeTrackers;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
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
    const string XRSETTINGS_DEVICE = "OpenVR";

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
    private bool _useWorldTransition = true;
    private bool _reloadLocalAvatar = true;

    // Internal
    private bool _switchInProgress = false;

    void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }
        Instance = this;
    }

    public void StartSwitch()
    {
        StartCoroutine(StartSwitchCoroutine());
    }

    private IEnumerator StartSwitchCoroutine()
    {
        if (_switchInProgress)
        {
            yield break;
        }
        _switchInProgress = true;
        yield return null;


        if (_useWorldTransition) // start visual transition and wait for it to complete
            yield return WorldTransitionSystem.Instance.StartTransitionCoroutine();

        // Check if OpenVR is running
        bool isUsingVr = IsInVR();

        InvokeOnPreSwitch(isUsingVr);

        // Start switch
        if (!isUsingVr)
        {
            yield return StartCoroutine(StartSteamVR());
        }
        else
        {
            StopSteamVR();
        }

        // Check for updated VR mode
        if (isUsingVr != IsInVR())
        {
            InvokeOnPostSwitch(!isUsingVr);

            // reload the local avatar
            // only reload on success
            if (_reloadLocalAvatar)
                Utils.ReloadLocalAvatar();
        }
        else
        {
            InvokeOnFailedSwitch(!isUsingVr);
        }

        if (_useWorldTransition) // finish the visual transition and wait
            yield return WorldTransitionSystem.Instance.ContinueTransitionCoroutine();

        _switchInProgress = false;
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

    public bool IsInVR() => XRSettings.enabled;

    private IEnumerator StartSteamVR()
    {
        XRSettings.LoadDeviceByName(XRSETTINGS_DEVICE);
        yield return null; // wait a frame before checking

        if (!string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            //SteamVR.Initialize is fucking useless
            SteamVR_Behaviour.Initialize(true);
            SteamVR_Behaviour.instance.InitializeSteamVR(true);
        }

        yield return null;
        yield break;
    }

    private void StopSteamVR()
    {
        // Forces SteamVR to reinitialize SteamVR_Input next switch
        SteamVR_ActionSet_Manager.DisableAllActionSets();
        SteamVR_Input.initialized = false;

        // Remove SteamVR
        DestroyImmediate(SteamVR_Behaviour.instance.gameObject);
        SteamVR.enabled = false;

        // Disable UnityXR
        XRSettings.LoadDeviceByName("");
        XRSettings.enabled = false;

        // We don't really need to wait on Stop()
    }
}