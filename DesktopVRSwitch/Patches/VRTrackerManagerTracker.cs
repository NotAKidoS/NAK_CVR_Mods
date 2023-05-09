using ABI_RC.Core.Player;
using HarmonyLib;
using UnityEngine;

namespace NAK.DesktopVRSwitch.Patches;

public class VRTrackerManagerTracker : MonoBehaviour
{
    public VRTrackerManager vrTrackerManager;

    void Start()
    {
        vrTrackerManager = GetComponent<VRTrackerManager>();
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }
    void OnDestroy()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PostVRModeSwitch(bool enableVR, Camera activeCamera)
    {
        //force the VRTrackerManager to reset anything its stored
        //this makes it get correct Left/Right hand if entering VR with different controllers
        //or if you restarted SteamVR and controllers are now in swapped index
        vrTrackerManager.poses = null;
        vrTrackerManager.leftHand = null;
        vrTrackerManager.rightHand = null;
        vrTrackerManager.hasCheckedForKnuckles = false;
    }
}