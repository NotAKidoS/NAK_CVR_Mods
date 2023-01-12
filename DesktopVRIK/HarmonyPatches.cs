using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.Player.AvatarTracking.Local;
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

namespace DesktopVRIK;

[HarmonyPatch]
internal class HarmonyPatches
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarGeneral")]
    private static void SetupDesktopIKSystem(ref CVRAvatar ____avatarDescriptor, ref Animator ____animator)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            if (____avatarDescriptor != null && ____animator != null && ____animator.isHuman)
            {
                //this will stop at the useless isVr return (the function is only ever called by vr anyways...)
                IKSystem.Instance.InitializeAvatar(____avatarDescriptor);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
    private static void InitializeDesktopAvatarVRIK(CVRAvatar avatar, ref VRIK ____vrik, ref HumanPoseHandler ____poseHandler, ref HumanPose ___humanPose)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            if (IKSystem.Instance.animator != null && IKSystem.Instance.animator.avatar != null && IKSystem.Instance.animator.avatar.isHuman)
            {
                //why the fuck does this fix bad armatures and heels in ground ??? (this one is suprisingly not because of Default Robot Kyle) ... (Fuck you Default Robot Kyle)
                if (____poseHandler == null)
                {
                    ____poseHandler = new HumanPoseHandler(IKSystem.Instance.animator.avatar, IKSystem.Instance.animator.transform);
                }
                ____poseHandler.GetHumanPose(ref ___humanPose);
                for (int i = 0; i < TPoseMuscles.Length; i++)
                {
                    IKSystem.Instance.ApplyMuscleValue((MuscleIndex)i, TPoseMuscles[i], ref ___humanPose.muscles);
                }
                ____poseHandler.SetHumanPose(ref ___humanPose);

                //need IKSystem to see VRIK component for setup
                if (____vrik == null)
                {
                    ____vrik = avatar.gameObject.AddComponent<VRIK>();
                }

                //now I calibrate DesktopVRIK
                DesktopVRIK.Instance.CalibrateDesktopVRIK(avatar);
            }
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
            IKSystem.vrik.transform.localPosition = Vector3.zero;
            IKSystem.vrik.transform.localRotation = Quaternion.identity;
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
            IKSystem.vrik.transform.localPosition = Vector3.zero;
            IKSystem.vrik.transform.localRotation = Quaternion.identity;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "HandleDesktopCameraPosition")]
    private static void Postfix_PlayerSetup_HandleDesktopCameraPosition(bool ignore, ref PlayerSetup __instance, ref MovementSystem ____movementSystem, ref int ___headBobbingLevel)
    {
        if (DesktopVRIK.Instance.Setting_EnforceViewPosition)
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

	private static readonly float[] TPoseMuscles = new float[]
	{
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0f,
	0.6001086f,
	8.6213E-05f,
	-0.0003308152f,
	0.9999163f,
	-9.559652E-06f,
	3.41413E-08f,
	-3.415095E-06f,
	-1.024528E-07f,
	0.6001086f,
	8.602679E-05f,
	-0.0003311098f,
	0.9999163f,
	-9.510122E-06f,
	1.707468E-07f,
	-2.732077E-06f,
	2.035554E-15f,
	-2.748694E-07f,
	2.619475E-07f,
	0.401967f,
	0.3005583f,
	0.04102772f,
	0.9998822f,
	-0.04634236f,
	0.002522987f,
	0.0003842837f,
	-2.369134E-07f,
	-2.232262E-07f,
	0.4019674f,
	0.3005582f,
	0.04103433f,
	0.9998825f,
	-0.04634996f,
	0.00252335f,
	0.000383302f,
	-1.52127f,
	0.2634507f,
	0.4322457f,
	0.6443988f,
	0.6669409f,
	-0.4663372f,
	0.8116828f,
	0.8116829f,
	0.6678119f,
	-0.6186608f,
	0.8116842f,
	0.8116842f,
	0.6677991f,
	-0.619225f,
	0.8116842f,
	0.811684f,
	0.6670032f,
	-0.465875f,
	0.811684f,
	0.8116836f,
	-1.520098f,
	0.2613016f,
	0.432256f,
	0.6444503f,
	0.6668426f,
	-0.4670413f,
	0.8116828f,
	0.8116828f,
	0.6677986f,
	-0.6192409f,
	0.8116841f,
	0.811684f,
	0.6677839f,
	-0.6198869f,
	0.8116839f,
	0.8116838f,
	0.6668782f,
	-0.4667901f,
	0.8116842f,
	0.811684f
	};

}