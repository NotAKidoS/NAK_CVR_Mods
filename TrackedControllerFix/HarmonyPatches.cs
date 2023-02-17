using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using HarmonyLib;
using Valve.VR;

namespace NAK.Melons.TrackedControllerFix.HarmonyPatches;

internal class PlayerSetupPatches
{
    public static SteamVR_Behaviour_Pose vrLeftHandPose;
    public static SteamVR_Behaviour_Pose vrRightHandPose;

    public static SteamVR_TrackedObject vrLeftHandTracker;
    public static SteamVR_TrackedObject vrRightHandTracker;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Post_PlayerSetup_Start(ref PlayerSetup __instance)
    {
        // Add SteamVR_TrackedObject and get SteamVR_Behaviour_Pose
        vrLeftHandTracker = __instance.vrLeftHandTracker.AddComponent<SteamVR_TrackedObject>();
        vrRightHandTracker = __instance.vrRightHandTracker.AddComponent<SteamVR_TrackedObject>();
        vrLeftHandPose = __instance.vrLeftHandTracker.GetComponent<SteamVR_Behaviour_Pose>();
        vrRightHandPose = __instance.vrRightHandTracker.GetComponent<SteamVR_Behaviour_Pose>();
        vrLeftHandPose.enabled = false;
        vrRightHandPose.enabled = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarVr")]
    private static void Prefix_PlayerSetup_SetupAvatarVr()
    {
        // This is a super lazy way of doing this...
        // but this is the best way to support DesktopVRSwitch & not redo the controller inputs
        if (vrLeftHandTracker != null)
        {
            vrLeftHandPose.enabled = true;
            vrLeftHandTracker.SetDeviceIndex(vrLeftHandPose.GetDeviceIndex());
            vrLeftHandPose.enabled = false;
        }
        if (vrRightHandTracker != null)
        {
            vrRightHandPose.enabled = true;
            vrRightHandTracker.SetDeviceIndex(vrRightHandPose.GetDeviceIndex());
            vrRightHandPose.enabled = false;
        }
    }
}