using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.Melons.IKFixes.HarmonyPatches;

internal static class BodySystemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), "SetupOffsets")]
    private static void Postfix_BodySystem_SetupOffsets(List<TrackingPoint> trackingPoints)
    {
        //redo offsets for knees as native is too far from pivot
        foreach (TrackingPoint trackingPoint in trackingPoints)
        {
            Transform parent = null;
            if (trackingPoint.assignedRole == TrackingPoint.TrackingRole.LeftKnee)
            {
                parent = IKSystem.vrik.references.leftCalf;
            }
            else if (trackingPoint.assignedRole == TrackingPoint.TrackingRole.RightKnee)
            {
                parent = IKSystem.vrik.references.rightCalf;
            }

            if (parent != null)
            {
                trackingPoint.offsetTransform.parent = parent;
                trackingPoint.offsetTransform.localPosition = Vector3.zero;
                trackingPoint.offsetTransform.localRotation = Quaternion.identity;
                trackingPoint.offsetTransform.parent = trackingPoint.referenceTransform;

                Vector3 b = IKSystem.vrik.references.root.forward * 0.5f;
                trackingPoint.offsetTransform.position += b;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), "Update")]
    private static bool Prefix_BodySystem_Update(ref BodySystem __instance)
    {
        if (IKSystem.vrik == null) return false;
        IKSolverVR solver = IKSystem.vrik.solver;

        // Allow avatar to rotate seperatly from Player (Desktop&VR)
        // FBT needs avatar root to follow head
        solver.spine.maxRootAngle = BodySystem.isCalibratedAsFullBody ? 0f : 180f;

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
                SetPelvisWeight(solver.spine, isRunning ? 0f : 1f);
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

        int num = 0;
        int count = IKSystem.Instance.AllTrackingPoints.FindAll((TrackingPoint m) => m.isActive && m.isValid && m.suggestedRole > TrackingPoint.TrackingRole.Invalid).Count;

        // fixes having all tracking points disabled forcing calibration
        if (count == 0)
        {
            __instance._fbtAvailable = false;
            return false;
        }

        // solid body count block
        if (BodySystem.enableLeftFootTracking) num++;
        if (BodySystem.enableRightFootTracking) num++;
        if (BodySystem.enableHipTracking) num++;
        if (BodySystem.enableLeftKneeTracking) num++;
        if (BodySystem.enableRightKneeTracking) num++;
        if (BodySystem.enableChestTracking) num++;
        if (BodySystem.enableLeftElbowTracking) num++;
        if (BodySystem.enableRightElbowTracking) num++;

        __instance._fbtAvailable = (count >= num);

        void SetArmWeight(IKSolverVR.Arm arm, float weight)
        {
            arm.positionWeight = weight;
            arm.rotationWeight = weight;
            arm.shoulderRotationWeight = weight;
            arm.shoulderTwistWeight = weight;
            // assumed fix of bend goal weight if arms disabled with elbows (havent tested)
            arm.bendGoalWeight = arm.bendGoal != null ? weight : 0f;
        }
        void SetLegWeight(IKSolverVR.Leg leg, float weight)
        {
            leg.positionWeight = weight;
            leg.rotationWeight = weight;
            // fixes knees bending to tracker if feet disabled (running anim)
            leg.bendGoalWeight = leg.bendGoal != null ? weight : 0f;
        }
        void SetPelvisWeight(IKSolverVR.Spine spine, float weight)
        {
            // looks better when hips are disabled while running
            if (spine.pelvisTarget != null)
            {
                spine.pelvisPositionWeight = weight;
                spine.pelvisRotationWeight = weight;
            }
            else
            {
                spine.pelvisPositionWeight = 0f;
                spine.pelvisRotationWeight = 0f;
            }
        }

        return false;
    }
}

internal static class VRIKPatches
{
    /**
        Leg solver uses virtual bone calf and foot, plus world tracked knee position for normal maths.
        This breaks as you playspace up, because calf and foot position aren't offset yet in solve order.
    **/

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), "ApplyOffsets")]
    private static bool Prefix_IKSolverVR_Leg_ApplyOffsets(ref IKSolverVR.Leg __instance)
    {
        //This is the second part of the above fix, preventing the solver from calculating a bad bendNormal
        //when it doesn't need to. The knee tracker should dictate the bendNormal completely.

        if (__instance.usingKneeTracker)
        {
            __instance.ApplyPositionOffset(__instance.footPositionOffset, 1f);
            __instance.ApplyRotationOffset(__instance.footRotationOffset, 1f);
            Quaternion quaternion = Quaternion.FromToRotation(__instance.footPosition - __instance.position, __instance.footPosition + __instance.heelPositionOffset - __instance.position);
            __instance.footPosition = __instance.position + quaternion * (__instance.footPosition - __instance.position);
            __instance.footRotation = quaternion * __instance.footRotation;
            return false;
        }

        // run full method like normal otherwise
        float num = __instance.bendGoalWeight;
        __instance.bendGoalWeight = 0f;
        __instance.ApplyOffsetsOld();
        __instance.bendGoalWeight = num;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSolverVR.Leg), "Solve")]
    private static void Prefix_IKSolverVR_Leg_Solve(ref IKSolverVR.Leg __instance)
    {
        //Turns out VRIK applies bend goal maths before root is offset in solving process.
        //We will apply ourselves before then to fix it.
        if (__instance.usingKneeTracker)
            __instance.ApplyBendGoal();
    }
}

internal static class PlayerSetupPatches
{
    // Last Movement Parent Info
    static Vector3 lastMovementPosition;
    static Quaternion lastMovementRotation;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), "ResetIk")]
    private static bool Prefix_PlayerSetup_ResetIk()
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

            // Add platform motion to IK solver
            IKSystem.vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, currentPosition);

            // Store for next frame
            lastMovementPosition = currentPosition;
            lastMovementRotation = currentRotation;
            return false;
        }

        return true;
    }
}