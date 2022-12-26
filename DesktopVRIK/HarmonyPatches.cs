using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using RootMotion.FinalIK;

namespace DesktopVRIK;

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarGeneral")]
    private static void SetupDesktopIKSystem(ref CVRAvatar ____avatarDescriptor)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            //this will stop at the useless isVr return (the function is only ever called by vr anyways...)
            IKSystem.Instance.InitializeAvatar(____avatarDescriptor);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
    private static void InitializeDesktopAvatarVRIK(CVRAvatar avatar, ref VRIK ____vrik)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            //need IKSystem to see VRIK component for setup
            ____vrik = avatar.gameObject.AddComponent<VRIK>();
            //now I calibrate DesktopVRIK
            DesktopVRIK.Instance.CalibrateAvatarVRIK(avatar);
        }
    }

    private static bool emotePlayed = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Update")]
    private static void CorrectVRIK(ref bool ____emotePlaying, ref LookAtIK ___lookIK)
    {
        if (MetaPort.Instance.isUsingVr || DesktopVRIK.Instance == null) return;

        //might need to rework this in the future
        if (____emotePlaying && !emotePlayed)
        {
            emotePlayed = true;
            if (DesktopVRIK.Instance.Setting_EmoteVRIK)
            {
                BodySystem.TrackingEnabled = false;
                //IKSystem.vrik.solver.Reset();
            }
            if (DesktopVRIK.Instance.Setting_EmoteLookAtIK && ___lookIK != null)
            {
                ___lookIK.enabled = false;
            }
        }
        else if (!____emotePlaying && emotePlayed)
        {
            emotePlayed = false;
            IKSystem.vrik.solver.Reset();
            BodySystem.TrackingEnabled = true;
            if (___lookIK != null)
            {
                ___lookIK.enabled = true;
            }
        }
    }
}