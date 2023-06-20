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
    public bool _useWorldTransition = true;
    public bool _reloadLocalAvatar = true;

    // Info
    public bool SwitchInProgress { get; private set; }

    void Awake()
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
            // reload the local avatar
            if (_reloadLocalAvatar)
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

        if (_useWorldTransition) // finish the visual transition and wait
            yield return WorldTransitionSystem.Instance.ContinueTransitionCoroutine();

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

        // Remove SteamVR behaviour & render
        DestroyImmediate(SteamVR_Behaviour.instance.gameObject);
        SteamVR.enabled = false; // disposes SteamVR

        // Disable UnityXR
        XRSettings.LoadDeviceByName("");
        XRSettings.enabled = false;

        // We don't really need to wait a frame on Stop()
    }
}