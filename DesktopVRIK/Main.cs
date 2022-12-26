using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace DesktopVRIK;

public class DesktopVRIK : MelonMod
{

    private static MelonPreferences_Category m_categoryDesktopVRIK;
    private static MelonPreferences_Entry<bool> m_entryEnabled;
    private static MelonPreferences_Entry<bool> m_entryEmulateHipMovement;
    private static MelonPreferences_Entry<bool> m_entryEmoteVRIK;
    private static MelonPreferences_Entry<bool> m_entryEmoteLookAtIK;

    public override void OnApplicationStart()
    {
        m_categoryDesktopVRIK = MelonPreferences.CreateCategory(nameof(DesktopVRIK));
        m_entryEnabled = m_categoryDesktopVRIK.CreateEntry<bool>("Enabled", true, description: "Attempt to give Desktop VRIK.");
        m_entryEmulateHipMovement = m_categoryDesktopVRIK.CreateEntry<bool>("Emulate Hip Movement", true, description: "Emulates VRChat-like hip movement when moving head up/down on desktop.");
        m_entryEmoteVRIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote VRIK", true, description: "Disable VRIK while emoting.");
        m_entryEmoteLookAtIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote LookAtIK", true, description: "Disable LookAtIK while emoting.");
    }

    [HarmonyPatch]
    private class HarmonyPatches
    {
        private static bool emotePlayed = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerSetup), "Update")]
        private static void CorrectVRIK(ref bool ____emotePlaying, ref LookAtIK ___lookIK)
        {
            if (MetaPort.Instance.isUsingVr) return;

            if (IKSystem.vrik == null) return;

            //pretty much zero out VRIK trying to locomote us using autofootstep
            IKSystem.Instance.avatar.transform.localPosition = Vector3.zero;
            IKSystem.Instance.avatar.transform.localRotation = Quaternion.identity;

            //TODO: Smooth out offset when walking/running
            if ( m_entryEmulateHipMovement.Value )
            {
                float angle = PlayerSetup.Instance.desktopCamera.transform.localEulerAngles.x;
                angle = (angle > 180) ? angle - 360 : angle;
                float weight = (1 - MovementSystem.Instance.movementVector.magnitude);
                Quaternion rotation = Quaternion.AngleAxis(angle * weight, IKSystem.Instance.avatar.transform.right);
                IKSystem.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Head, rotation);
            }


            //Avatar Motion Tweaker has custom emote detection to disable VRIK via state tags
            if (____emotePlaying && !emotePlayed)
            {
                emotePlayed = true;
                if (m_entryEmoteVRIK.Value)
                {
                    BodySystem.TrackingEnabled = false; 
                    IKSystem.vrik.solver.Reset();
                }
                if (m_entryEmoteLookAtIK.Value && ___lookIK != null)
                {
                    ___lookIK.enabled = false;
                }
            }
            else if (!____emotePlaying && emotePlayed)
            {
                emotePlayed = false;
                BodySystem.TrackingEnabled = true;
                IKSystem.vrik.solver.Reset();
                if (___lookIK != null)
                {
                    ___lookIK.enabled = true;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
        private static void InitializeDesktopAvatar(CVRAvatar avatar, ref VRIK ____vrik, ref HumanPoseHandler ____poseHandler, ref Vector3 ____referenceRootPosition, ref Quaternion ____referenceRootRotation, ref float[] ___HandCalibrationPoseMuscles)
        {
            if (!m_entryEnabled.Value) return;

            if (MetaPort.Instance.isUsingVr) return;

            //set avatar to 
            Quaternion initialRotation = avatar.transform.rotation;
            avatar.transform.rotation = Quaternion.identity;

            ____vrik = avatar.gameObject.AddComponent<VRIK>();
            ____vrik.fixTransforms = false;
            ____vrik.solver.locomotion.weight = 1f;
            IKSystem.Instance.ApplyAvatarScaleToIk(avatar.viewPosition.y);
            ____vrik.solver.locomotion.angleThreshold = 30f;
            ____vrik.solver.locomotion.maxLegStretch = 0.75f;
            ____vrik.solver.spine.headClampWeight = 0f;
            ____vrik.solver.spine.minHeadHeight = 0f;
            if (____vrik != null)
            {
                ____vrik.onPreSolverUpdate.AddListener(new UnityAction(IKSystem.Instance.OnPreSolverUpdate));
            }

            if (____poseHandler == null)
            {
                ____poseHandler = new HumanPoseHandler(IKSystem.Instance.animator.avatar, IKSystem.Instance.animator.transform);
            }
            ____poseHandler.GetHumanPose(ref IKSystem.Instance.humanPose);
            ____referenceRootPosition = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Hips).position;
            ____referenceRootRotation = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
            for (int i = 0; i < ___HandCalibrationPoseMuscles.Length; i++)
            {
                IKSystem.Instance.ApplyMuscleValue((MuscleIndex)i, ___HandCalibrationPoseMuscles[i], ref IKSystem.Instance.humanPose.muscles);
            }
            ____poseHandler.SetHumanPose(ref IKSystem.Instance.humanPose);
            if (IKSystem.Instance.applyOriginalHipPosition)
            {
                IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Hips).position = ____referenceRootPosition;
            }
            if (IKSystem.Instance.applyOriginalHipRotation)
            {
                IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Hips).rotation = ____referenceRootRotation;
            }

            BodySystem.isCalibratedAsFullBody = false;
            BodySystem.isCalibrating = false;
            BodySystem.TrackingPositionWeight = 1f;
            BodySystem.isCalibratedAsFullBody = false;

            //InitializeDesktopIK

            //centerEyeAnchor now is head bone
            Transform headAnchor = FindIKTarget(IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.Head));
            IKSystem.Instance.headAnchorPositionOffset = Vector3.zero;
            IKSystem.Instance.headAnchorRotationOffset = Vector3.zero;

            IKSystem.Instance.leftHandModel.SetActive(false);
            IKSystem.Instance.rightHandModel.SetActive(false);
            IKSystem.Instance.animator = IKSystem.Instance.avatar.GetComponent<Animator>();
            IKSystem.Instance.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            //tell game to not track limbs
            BodySystem.TrackingLeftArmEnabled = false;
            BodySystem.TrackingRightArmEnabled = false;
            BodySystem.TrackingLeftLegEnabled = false;
            BodySystem.TrackingRightLegEnabled = false;

            //create ik targets for avatars to utilize
            //____vrik.solver.leftLeg.target = FindIKTarget(IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.LeftFoot));
            //____vrik.solver.rightLeg.target = FindIKTarget(IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.RightFoot));

            ____vrik.solver.IKPositionWeight = 0f;
            ____vrik.enabled = false;

            VRIKCalibrator.CalibrateHead(____vrik, headAnchor, IKSystem.Instance.headAnchorPositionOffset, IKSystem.Instance.headAnchorRotationOffset);
            //IKSystem.Instance.leftHandPose.transform.position = ____vrik.references.leftHand.position;
            //IKSystem.Instance.rightHandPose.transform.position = ____vrik.references.rightHand.position;
            //____vrik.solver.leftArm.target = IKSystem.Instance.leftHandAnchor;
            //____vrik.solver.rightArm.target = IKSystem.Instance.rightHandAnchor;

            //Transform boneTransform = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            //Transform boneTransform2 = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            //if (boneTransform.GetComponent<TwistRelaxer>() == null)
            //{
            //    TwistRelaxer twistRelaxer = boneTransform.gameObject.AddComponent<TwistRelaxer>();
            //    twistRelaxer.ik = ____vrik;
            //    twistRelaxer.child = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.LeftHand);
            //    twistRelaxer.weight = 0.5f;
            //    twistRelaxer.parentChildCrossfade = 0.9f;
            //    twistRelaxer.twistAngleOffset = 0f;
            //}
            //if (boneTransform2.GetComponent<TwistRelaxer>() == null)
            //{
            //    TwistRelaxer twistRelaxer2 = boneTransform2.gameObject.AddComponent<TwistRelaxer>();
            //    twistRelaxer2.ik = ____vrik;
            //    twistRelaxer2.child = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.RightHand);
            //    twistRelaxer2.weight = 0.5f;
            //    twistRelaxer2.parentChildCrossfade = 0.9f;
            //    twistRelaxer2.twistAngleOffset = 0f;
            //}
            //if (IKSystem.Instance.animator != null && IKSystem.Instance.animator.avatar != null && IKSystem.Instance.animator.avatar.isHuman)
            //{
            //    Transform boneTransform3 = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
            //    if (boneTransform3 != null)
            //    {
            //        ____vrik.solver.leftArm.palmToThumbAxis = VRIKCalibrator.GuessPalmToThumbAxis(____vrik.references.leftHand, ____vrik.references.leftForearm, boneTransform3);
            //    }
            //    Transform boneTransform4 = IKSystem.Instance.animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            //    if (boneTransform4 != null)
            //    {
            //        ____vrik.solver.rightArm.palmToThumbAxis = VRIKCalibrator.GuessPalmToThumbAxis(____vrik.references.rightHand, ____vrik.references.rightForearm, boneTransform4);
            //    }
            //}

            ____vrik.enabled = true;
            ____vrik.solver.IKPositionWeight = 1f;
            ____vrik.solver.spine.maintainPelvisPosition = 0f;

            //prevent the IK from walking away
            ____vrik.solver.locomotion.maxVelocity = 0f;
            avatar.transform.rotation = initialRotation;
        }

        private static Transform FindIKTarget(Transform targetParent)
        {
            /**
				I want creators to be able to specify their own custom IK Targets, so they can move them around with animations if they want.
				We check for existing target objects, and if none are found we make our own.
				Naming scheme is parentobject name + " IK Target".
			**/
            Transform parentTransform = targetParent.transform;
            string targetName = parentTransform.name + " IK Target";

            //check for existing target
            foreach (object obj in parentTransform)
            {
                Transform childTransform = (Transform)obj;
                if (childTransform.name == targetName)
                {
                    return childTransform;
                }
            }

            //create new target if none are found
            GameObject newTarget = new GameObject(targetName);
            newTarget.transform.parent = parentTransform;
            newTarget.transform.localPosition = Vector3.zero;
            newTarget.transform.localRotation = Quaternion.identity;
            return newTarget.transform;
        }
    }
}