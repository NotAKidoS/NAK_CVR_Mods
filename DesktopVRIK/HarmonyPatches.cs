using ABI_RC.Core.Player;
using ABI.CCK.Components;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;

namespace DesktopVRIK;

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarGeneral")]
    private static void InitializeDesktopIKSystem(ref CVRAvatar ____avatarDescriptor)
    {
        if (MetaPort.Instance.isUsingVr) return;

        //this will stop at the useless isVr return (the function is only ever called by vr anyways...)
        IKSystem.Instance.InitializeAvatar(____avatarDescriptor);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
    private static void InitializeDesktopAvatar(CVRAvatar avatar, ref VRIK ____vrik)
    {
        //need IKSystem to see VRIK component for setup
        ____vrik = avatar.gameObject.AddComponent<VRIK>();
        //now i add my own VRIK stuff
        NAKDesktopVRIK NAKVRIK = avatar.gameObject.AddComponent<NAKDesktopVRIK>();
        NAKVRIK.CalibrateAvatarVRIK(avatar);
    }
}