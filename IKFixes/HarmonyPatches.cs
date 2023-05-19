using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace NAK.IKFixes.HarmonyPatches;

internal static class BodySystemPatches
{
    static float _ikSimulatedRootAngle = 0f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.SetupOffsets))]
    static void Postfix_BodySystem_SetupOffsets(List<TrackingPoint> trackingPoints)
    {
        foreach (TrackingPoint trackingPoint in trackingPoints)
        {
            Transform parent = null;
            float offsetDistance = 0f;

            switch (trackingPoint.assignedRole)
            {
                case TrackingPoint.TrackingRole.LeftKnee:
                    parent = IKSystem.vrik.references.leftCalf;
                    offsetDistance = 0.15f;
                    break;
                case TrackingPoint.TrackingRole.RightKnee:
                    parent = IKSystem.vrik.references.rightCalf;
                    offsetDistance = 0.15f;
                    break;
                case TrackingPoint.TrackingRole.LeftElbow:
                    parent = IKSystem.vrik.references.leftForearm;
                    offsetDistance = -0.15f;
                    break;
                case TrackingPoint.TrackingRole.RightElbow:
                    parent = IKSystem.vrik.references.rightForearm;
                    offsetDistance = -0.15f;
                    break;
                default:
                    break;
            }

            if (parent != null)
            {
                // Set the offset transform's parent and reset its local position and rotation
                trackingPoint.offsetTransform.parent = parent;
                trackingPoint.offsetTransform.localPosition = Vector3.zero;
                trackingPoint.offsetTransform.localRotation = Quaternion.identity;
                trackingPoint.offsetTransform.parent = trackingPoint.referenceTransform;

                // Apply additional offset based on the assigned role
                Vector3 additionalOffset = IKSystem.vrik.references.root.forward * offsetDistance;
                trackingPoint.offsetTransform.position += additionalOffset;

                // Game originally sets them to about half a meter out, which fucks with slime tracker users and
                // makes the bendGoals less responsive/less accurate. 

                //Funny thing is that IKTweaks specifically made this an option, which should be added to both CVR & Standable for the same reason.
                /// Elbow / knee / chest bend goal offset - controls how far bend goal targets will be away from the actual joint.
                /// Lower values should produce better precision with bent joint, higher values - better stability with straight joint. 
                /// Sensible range of values is between 0 and 1.
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.Update))]
    static bool Prefix_BodySystem_Update(ref BodySystem __instance)
    {
        void SetArmWeight(IKSolverVR.Arm arm, float weight)
        {
            arm.positionWeight = weight;
            arm.rotationWeight = weight;
            arm.shoulderRotationWeight = weight;
            arm.shoulderTwistWeight = weight;
            // assumed fix of bend goal weight if arms disabled with elbows (havent tested)
            // why is there no "usingElbowTracker" flag like knees? where is the consistancy???
            arm.bendGoalWeight = arm.bendGoal != null ? weight : 0f;
        }
        void SetLegWeight(IKSolverVR.Leg leg, float weight)
        {
            leg.positionWeight = weight;
            leg.rotationWeight = weight;
            // fixes knees bending to tracker if feet disabled (running anim)
            leg.bendGoalWeight = leg.usingKneeTracker ? 0.9f : 0f;
        }
        void SetPelvisWeight(IKSolverVR.Spine spine, float weight)
        {
            // looks better when hips are disabled while running
            spine.pelvisPositionWeight = weight;
            spine.pelvisRotationWeight = weight;
        }

        if (IKSystem.vrik != null)
        {
            IKSolverVR solver = IKSystem.vrik.solver;

            if (BodySystem.TrackingEnabled)
            {
                IKSystem.vrik.enabled = true;
                solver.IKPositionWeight = BodySystem.TrackingPositionWeight;
                solver.locomotion.weight = BodySystem.TrackingLocomotionEnabled ? 1f : 0f;

                // fixes arm weights not being set if leftArm & rightArm targets are null
                // game handles TrackingLegs in PlayerSetup, but not for knee goals
                SetArmWeight(solver.leftArm, BodySystem.TrackingLeftArmEnabled && solver.leftArm.target != null ? 1f : 0f);
                SetArmWeight(solver.rightArm, BodySystem.TrackingRightArmEnabled && solver.rightArm.target != null ? 1f : 0f);
                SetLegWeight(solver.leftLeg, BodySystem.TrackingLeftLegEnabled && solver.leftLeg.target != null ? 1f : 0f);
                SetLegWeight(solver.rightLeg, BodySystem.TrackingRightLegEnabled && solver.leftLeg.target != null ? 1f : 0f);
                SetPelvisWeight(solver.spine, solver.spine.pelvisTarget != null ? 1f : 0f);

                // makes running animation look better
                if (BodySystem.isCalibratedAsFullBody)
                {
                    bool isRunning = BodySystem.PlayRunningAnimationInFullBody && MovementSystem.Instance.movementVector.magnitude > 0f;
                    if (isRunning) SetPelvisWeight(solver.spine, 0f);
                }
            }
            else
            {
                IKSystem.vrik.enabled = false;
                solver.IKPositionWeight = 0f;
                solver.locomotion.weight = 0f;

                SetArmWeight(solver.leftArm, 0f);
                SetArmWeight(solver.rightArm, 0f);
                SetLegWeight(solver.leftLeg, 0f);
                SetLegWeight(solver.rightLeg, 0f);
                SetPelvisWeight(solver.spine, 0f);
            }

            float maxRootAngle = BodySystem.isCalibratedAsFullBody || IKFixes.EntryUseFakeRootAngle.Value ? (PlayerSetup.Instance._emotePlaying ? 180f : 0f) : (PlayerSetup.Instance._emotePlaying ? 180f : 25f);
            solver.spine.maxRootAngle = maxRootAngle;

            if (IKFixes.EntryUseFakeRootAngle.Value)
            {
                // Emulate maxRootAngle because CVR doesn't have the player controller set up ideally for VRIK.
                // I believe they'd need to change which object vrik.references.root is, as using avatar object is bad!
                // This is a small small fix, but makes it so the feet dont point in the direction of the head
                // when turning. It also means turning with joystick & turning IRL make feet behave the same and follow behind.
                float weightedAngleLimit = IKFixes.EntryFakeRootAngleLimit.Value * solver.locomotion.weight;
                float pivotAngle = MovementSystem.Instance.rotationPivot.eulerAngles.y;
                float deltaAngleRoot = Mathf.DeltaAngle(pivotAngle, _ikSimulatedRootAngle);
                float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);

                if (absDeltaAngleRoot > weightedAngleLimit)
                {
                    deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                    _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, pivotAngle, absDeltaAngleRoot - weightedAngleLimit);
                }
                solver.spine.rootHeadingOffset = deltaAngleRoot;
            }

            // custom IK settings
            solver.spine.neckStiffness = IKFixes.EntryNeckStiffness.Value;
            solver.spine.bodyRotStiffness = IKFixes.EntryBodyRotStiffness.Value;
            solver.spine.rotateChestByHands = IKFixes.EntryRotateChestByHands.Value;
        }

        int count = IKSystem.Instance.AllTrackingPoints.FindAll((TrackingPoint m) => m.isActive && m.isValid && m.suggestedRole > TrackingPoint.TrackingRole.Invalid).Count;

        // fixes having all tracking points disabled forcing calibration
        if (count == 0)
        {
            __instance._fbtAvailable = false;
            return false;
        }

        // solid body count block
        int num = 0;
        if (BodySystem.enableLeftFootTracking) num++;
        if (BodySystem.enableRightFootTracking) num++;
        if (BodySystem.enableHipTracking) num++;
        if (BodySystem.enableLeftKneeTracking) num++;
        if (BodySystem.enableRightKneeTracking) num++;
        if (BodySystem.enableChestTracking) num++;
        if (BodySystem.enableLeftElbowTracking) num++;
        if (BodySystem.enableRightElbowTracking) num++;

        __instance._fbtAvailable = (count >= num);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.AssignRemainingTrackers))]
    static bool Prefix_BodySystem_AssignRemainingTrackers()
    {
        return IKFixes.EntryAssignRemainingTrackers.Value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.MuscleUpdate))]
    static void Prefix_BodySystem_MuscleUpdate()
    {
        if (BodySystem.isCalibrating)
        {
            IKSystem.Instance.humanPose.bodyRotation = Quaternion.identity;
            IKSystem.vrik.solver.spine.maxRootAngle = 0f; // idk, testing
        }

        if (BodySystem.isCalibratedAsFullBody && BodySystem.TrackingPositionWeight > 0f)
        {
            bool isRunning = BodySystem.PlayRunningAnimationInFullBody && MovementSystem.Instance.movementVector.magnitude > 0f;
            if (!isRunning)
            {
                // Resetting bodyRotation made running animations look funky
                IKSystem.Instance.applyOriginalHipPosition = true;
                IKSystem.Instance.applyOriginalHipRotation = false;
                IKSystem.Instance.humanPose.bodyRotation = Quaternion.identity;
            }
            else
            {
                // This looks much better when running
                IKSystem.Instance.applyOriginalHipPosition = true;
                IKSystem.Instance.applyOriginalHipRotation = true;
            }
        }

        // TODO: Rewrite to exclude setting T-pose to limbs that are not tracked
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.Calibrate))]
    static void Postfix_BodySystem_Calibrate()
    {
        IKSystem.Instance.applyOriginalHipPosition = false;
        IKSystem.Instance.applyOriginalHipRotation = false;
        IKSystem.vrik.solver.leftLeg.bendToTargetWeight = 0.1f;
        IKSystem.vrik.solver.rightLeg.bendToTargetWeight = 0.1f;
        IKSystem.vrik.solver.leftLeg.bendGoalWeight = 0.9f;
        IKSystem.vrik.solver.rightLeg.bendGoalWeight = 0.9f;
    }
}

internal static class IKSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.InitializeAvatar))]
    static void Prefix_IKSystem_InitializeAvatar(ref IKSystem __instance)
    {
        __instance.applyOriginalHipPosition = true;
        __instance.applyOriginalHipRotation = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.InitializeHalfBodyIK))]
    static void Prefix_IKSystem_InitializeHalfBodyIK(IKSystem __instance)
    {
        if (IKSystem._vrik != null)
        {
            UnityAction onPostSolverUpdate = null;
            onPostSolverUpdate = () =>
            {
                if (!IKFixes.EntryNetIKPass.Value) return;

                Transform hips = __instance.animator.GetBoneTransform(HumanBodyBones.Hips);
                __instance._referenceRootPosition = hips.position;
                __instance._referenceRootRotation = hips.rotation;
                __instance._poseHandler.GetHumanPose(ref __instance.humanPose);
                __instance._poseHandler.SetHumanPose(ref __instance.humanPose);
                hips.position = __instance._referenceRootPosition;
                hips.rotation = __instance._referenceRootRotation;
            };

            IKSystem._vrik.onPostSolverUpdate.AddListener(onPostSolverUpdate);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.InitializeHalfBodyIK))]
    static void Postfix_IKSystem_InitializeHalfBodyIK(ref IKSystem __instance)
    {
        if (!IKFixes.EntryUseIKPose.Value) return;

        __instance._poseHandler.GetHumanPose(ref __instance.humanPose);

        for (int i = 0; i < IKPoseMuscles.Length; i++)
        {
            __instance.ApplyMuscleValue((MuscleIndex)i, IKPoseMuscles[i], ref __instance.humanPose.muscles);
        }
        __instance.humanPose.bodyPosition = Vector3.up;
        __instance.humanPose.bodyRotation = Quaternion.identity;
        __instance._poseHandler.SetHumanPose(ref __instance.humanPose);

        // recentering avatar so it doesnt need to step from random place on switch
        IKSystem.vrik.transform.localPosition = Vector3.zero;
        IKSystem.vrik.transform.localRotation = Quaternion.identity;
        // janky fix, initializing early with correct pose
        IKSystem.vrik.solver.Initiate(IKSystem.vrik.transform);
    } 

    static readonly float[] IKPoseMuscles = new float[]
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

internal static class VRIKPatches
{
    /**
        Leg solver uses virtual bone calf and foot, plus world tracked knee position for normal maths.
        This breaks as you playspace up, because calf and foot position aren't offset yet in solve order.
    **/

    // Add ApplyBendGoal(); to the second line of RootMotionNew.FinalIK.IKSolverVR.Leg.Solve(bool)
    // https://github.com/knah/VRCMods/tree/b6c4198fb8e06174ea511fe1f8a3257dfef2fdd2 IKTweaks

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), nameof(IKSolverVR.Leg.Stretching))]
    static void Postfix_IKSolverVR_Leg_Stretching(ref IKSolverVR.Leg __instance)
    {
        // I am patching here because Stretching() is always called
        // and i am not doing a transpiler to place it on the second line
        __instance.ApplyBendGoal();
    }

    // IKTweaks original method does not do this?
    // idk i prefer it, upper leg rotates weird otherwise

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), nameof(IKSolverVR.Leg.ApplyOffsets))]
    static bool Prefix_IKSolverVR_Leg_ApplyOffsets(ref IKSolverVR.Leg __instance)
    {
        // Apply position and rotation offsets
        __instance.ApplyPositionOffset(__instance.footPositionOffset, 1f);
        __instance.ApplyRotationOffset(__instance.footRotationOffset, 1f);

        // Calculate new foot position and rotation
        Quaternion footQuaternion = Quaternion.FromToRotation(__instance.footPosition - __instance.position, __instance.footPosition + __instance.heelPositionOffset - __instance.position);
        __instance.footPosition = __instance.position + footQuaternion * (__instance.footPosition - __instance.position);
        __instance.footRotation = footQuaternion * __instance.footRotation;

        if (__instance.usingKneeTracker)
        {
            float angle = 0f;
            if (__instance.bendGoal != null && __instance.bendGoalWeight > 0f)
            {
                Vector3 crossProduct = Vector3.Cross(__instance.bendGoal.position - __instance.thigh.solverPosition, __instance.position - __instance.thigh.solverPosition);
                Vector3 rotatedPoint = Quaternion.Inverse(Quaternion.LookRotation(__instance.bendNormal, __instance.thigh.solverPosition - __instance.foot.solverPosition)) * crossProduct;
                angle = Mathf.Atan2(rotatedPoint.x, rotatedPoint.z) * Mathf.Rad2Deg * __instance.bendGoalWeight;
            }

            // Adjust bend normal and thigh rotation for knee tracker
            // Knee tracker should take priority over swivelOffset
            __instance.bendNormal = Quaternion.AngleAxis(angle, __instance.thigh.solverPosition - __instance.lastBone.solverPosition) * __instance.bendNormal;
            __instance.thigh.solverRotation = Quaternion.AngleAxis(-angle, __instance.thigh.solverRotation * __instance.thigh.axis) * __instance.thigh.solverRotation;
            return false;
        }

        // Adjust bend normal and thigh rotation for swivel offset
        float adjustedAngle = __instance.swivelOffset;
        if (adjustedAngle > 90f) adjustedAngle = 180f - adjustedAngle;
        if (adjustedAngle < -90f) adjustedAngle = -180f - adjustedAngle;
        if (adjustedAngle != 0f)
        {
            __instance.bendNormal = Quaternion.AngleAxis(adjustedAngle, __instance.thigh.solverPosition - __instance.lastBone.solverPosition) * __instance.bendNormal;
            __instance.thigh.solverRotation = Quaternion.AngleAxis(-adjustedAngle, __instance.thigh.solverRotation * __instance.thigh.axis) * __instance.thigh.solverRotation;
        }

        return false;
    }
}

internal static class PlayerSetupPatches
{
    // Last Movement Parent Info
    static CVRMovementParent lastMovementParent;
    static Vector3 lastMovementPosition;
    static Quaternion lastMovementRotation;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ResetIk))]
    static bool Prefix_PlayerSetup_ResetIk()
    {
        if (IKSystem.vrik == null) return false;

        CVRMovementParent currentParent = MovementSystem.Instance._currentParent;
        if (currentParent != null && currentParent._referencePoint != null)
        {
            // Get current position, VR pivots around VR camera
            Vector3 currentPosition = MovementSystem.Instance.rotationPivot.transform.position;
            currentPosition.y = IKSystem.vrik.transform.position.y; // set pivot to floor
            Quaternion currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

            // Convert to delta position (how much changed since last frame)
            Vector3 deltaPosition = currentPosition - lastMovementPosition;
            Quaternion deltaRotation = Quaternion.Inverse(lastMovementRotation) * currentRotation;

            // Prevent targeting other parent position
            if (lastMovementParent == currentParent || lastMovementParent == null)
            {
                // Add platform motion to IK solver
                IKSystem.vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, currentPosition);
            }

            // Store for next frame
            lastMovementParent = currentParent;
            lastMovementPosition = currentPosition;
            lastMovementRotation = currentRotation;
            return false;
        }

        return true;
    }
}