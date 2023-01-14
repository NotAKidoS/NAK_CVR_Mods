using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

/**

	The process of calibrating VRIK is fucking painful.
	
	Immediatly doing GetHumanPose() and then SetHumanPose() fixed heels in ground for all avatars.

	Setting the avatars rotation to identity, head rotation offset to head bone world rotation, and then calibrating head IK target (kinda) fixed only robot kyle.

	Enforcing a TPose only fixed my ferret avatars right shoulder.

	Mix and matching these, and changing order, fucks with random specific avatars with fucky armatures.
	MOST AVATARS DONT EVEN CHANGE, ITS JUST THESE FEW SPECIFIC ONES
	I NEED to look into an IKPose controller...

	Avatars of Note:
		TurtleNeck Ferret- broken/inverted right shoulder
		Space Robot Kyle- head ik target is rotated -90 90 0, so body/neck is fucked (Fuck you Default Robot Kyle)
		Exteratta- the knees bend backwards like a fucking chicken... what the fuck im enforcing a tpose nowww

	Most other avatars play just fine. Never changes even when adding Tpose, rotating the avatar, headikrotationoffset, ect...
	WHY (Fuck you Default Robot Kyle)

**/

namespace NAK.Melons.DesktopVRIK.HarmonyPatches;

class PlayerSetupPatches
{
    private static bool emotePlayed = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarGeneral")]
    static void SetupDesktopIKSystem(ref CVRAvatar ____avatarDescriptor, ref Animator ____animator)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Setting_Enabled)
        {
            if (____avatarDescriptor != null && ____animator != null && ____animator.isHuman)
            {
                //this will stop at the useless isVr return (the function is only ever called by vr anyways...)
                IKSystem.Instance.InitializeAvatar(____avatarDescriptor);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Update")]
    private static void CorrectVRIK(ref bool ____emotePlaying, ref LookAtIK ___lookIK)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Setting_Enabled)
        {
            bool changed = ____emotePlaying != emotePlayed;
            if (changed)
            {
                emotePlayed = ____emotePlaying;
                IKSystem.vrik.transform.localPosition = Vector3.zero;
                IKSystem.vrik.transform.localRotation = Quaternion.identity;
                if (DesktopVRIK.Setting_EmoteLookAtIK && ___lookIK != null)
                {
                    ___lookIK.enabled = !____emotePlaying;
                }
                if (DesktopVRIK.Setting_EmoteVRIK)
                {
                    BodySystem.TrackingEnabled = !____emotePlaying;
                    IKSystem.vrik.solver?.Reset();
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "HandleDesktopCameraPosition")]
    private static void Postfix_PlayerSetup_HandleDesktopCameraPosition(bool ignore, ref PlayerSetup __instance, ref MovementSystem ____movementSystem, ref int ___headBobbingLevel)
    {
        if (DesktopVRIK.Setting_Enabled && DesktopVRIK.Setting_EnforceViewPosition)
        {
            if (!____movementSystem.disableCameraControl || ignore)
            {
                if (___headBobbingLevel == 2 && DesktopVRIK.Instance.viewpoint != null)
                {
                    __instance.desktopCamera.transform.localPosition = Vector3.zero;
                    __instance.desktopCameraRig.transform.position = DesktopVRIK.Instance.viewpoint.position;
                }
            }
        }
    }
}

class IKSystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
    private static void InitializeDesktopAvatarVRIK(CVRAvatar avatar, ref VRIK ____vrik, ref HumanPoseHandler ____poseHandler, ref HumanPose ___humanPose)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Setting_Enabled)
        {
            if (IKSystem.Instance.animator != null && IKSystem.Instance.animator.avatar != null && IKSystem.Instance.animator.avatar.isHuman)
            {
                if (____poseHandler == null)
                {
                    ____poseHandler = new HumanPoseHandler(IKSystem.Instance.animator.avatar, IKSystem.Instance.animator.transform);
                }

                ____poseHandler.GetHumanPose(ref ___humanPose);
                for (int i = 0; i < IKPoseMuscles.Length; i++)
                {
                    IKSystem.Instance.ApplyMuscleValue((MuscleIndex)i, IKPoseMuscles[i], ref ___humanPose.muscles);
                }
                ____poseHandler.SetHumanPose(ref ___humanPose);

                ____vrik = DesktopVRIK.Instance.AlternativeCalibration(avatar);
                IKSystem.Instance.ApplyAvatarScaleToIk(avatar.viewPosition.y);
            }
        }
    }

    private static readonly float[] IKPoseMuscles = new float[]
    {
            0.00133321f,
            8.195831E-06f,
            8.537738E-07f,
            -0.002669832f,
            -7.651234E-06f,
            -0.001659694f,
            0f,
            0f,
            0f,
            0.04213953f,
            0.0003007996f,
            -0.008032114f,
            -0.03059979f,
            -0.0003182998f,
            0.009640567f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0.5768794f,
            0.01061097f,
            -0.1127839f,
            0.9705755f,
            0.07972051f,
            -0.0268422f,
            0.007237188f,
            0f,
            0.5768792f,
            0.01056608f,
            -0.1127519f,
            0.9705756f,
            0.07971933f,
            -0.02682396f,
            0.007229362f,
            0f,
            -5.651802E-06f,
            -3.034899E-07f,
            0.4100508f,
            0.3610304f,
            -0.0838329f,
            0.9262537f,
            0.1353517f,
            -0.03578902f,
            0.06005657f,
            -4.95989E-06f,
            -1.43007E-06f,
            0.4096187f,
            0.363263f,
            -0.08205152f,
            0.9250782f,
            0.1345718f,
            -0.03572125f,
            0.06055461f,
            -1.079177f,
            0.2095419f,
            0.6140652f,
            0.6365265f,
            0.6683931f,
            -0.4764312f,
            0.8099416f,
            0.8099371f,
            0.6658203f,
            -0.7327053f,
            0.8113618f,
            0.8114051f,
            0.6643661f,
            -0.40341f,
            0.8111364f,
            0.8111367f,
            0.6170399f,
            -0.2524227f,
            0.8138723f,
            0.8110135f,
            -1.079171f,
            0.2095456f,
            0.6140658f,
            0.6365255f,
            0.6683878f,
            -0.4764301f,
            0.8099402f,
            0.8099376f,
            0.6658241f,
            -0.7327023f,
            0.8113653f,
            0.8113793f,
            0.664364f,
            -0.4034042f,
            0.811136f,
            0.8111364f,
            0.6170469f,
            -0.2524345f,
            0.8138595f,
            0.8110138f
    };
}
