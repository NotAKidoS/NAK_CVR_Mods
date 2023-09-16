using ABI_RC.Core.InteractionSystem;
using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.IKFixes.HarmonyPatches;

internal static class BodySystemPatches
{
    private static float _ikSimulatedRootAngle;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.SetupOffsets))]
    private static void Postfix_BodySystem_SetupOffsets(List<TrackingPoint> trackingPoints)
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
            }
    
            if (parent == null) 
                continue;
            
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

    private static void SetArmWeight(IKSolverVR.Arm arm, float weight)
    {
        arm.positionWeight = weight;
        arm.rotationWeight = weight;
        arm.shoulderRotationWeight = weight;
        arm.shoulderTwistWeight = weight;
        // assumed fix of bend goal weight if arms disabled with elbows (havent tested)
        arm.bendGoalWeight = arm.bendGoal != null ? weight : 0f;
    }

    private static void SetLegWeight(IKSolverVR.Leg leg, float weight)
    {
        leg.positionWeight = weight;
        leg.rotationWeight = weight;
        // fixes knees bending to tracker if feet disabled (running anim)
        leg.bendGoalWeight = leg.usingKneeTracker ? weight : 0f;
    }

    private static void SetPelvisWeight(IKSolverVR.Spine spine, float weight)
    {
        // looks better when hips are disabled while running
        spine.pelvisPositionWeight = weight;
        spine.pelvisRotationWeight = weight;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.Update))]
    private static bool Prefix_BodySystem_Update(ref BodySystem __instance)
    {
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

            float maxRootAngle = 25f;
            float rootHeadingOffset = 0f;

            if (BodySystem.isCalibratedAsFullBody 
                || IKFixes.EntryUseFakeRootAngle.Value
                ||  CVRInputManager.Instance.movementVector.sqrMagnitude > 0f)
                maxRootAngle = 0f;

            // fixes body being wrong direction while playing emotes (root rotation)
            if (PlayerSetup.Instance._emotePlaying)
                maxRootAngle = 180f;
            
            // fixes feet always pointing toward head direction
            if (IKFixes.EntryUseFakeRootAngle.Value && !BodySystem.isCalibratedAsFullBody)
            {
                float weightedAngleLimit = IKFixes.EntryFakeRootAngleLimit.Value * solver.locomotion.weight;
                float playerDirection = MovementSystem.Instance.rotationPivot.eulerAngles.y;
                
                float deltaAngleRoot = Mathf.DeltaAngle(playerDirection, _ikSimulatedRootAngle);
                float angleOverLimit = Mathf.Abs(deltaAngleRoot) - weightedAngleLimit;

                if (angleOverLimit > 0)
                {
                    deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                    _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, playerDirection, angleOverLimit);
                }
                
                rootHeadingOffset = deltaAngleRoot;
            }
            
            solver.spine.maxRootAngle = maxRootAngle;
            solver.spine.rootHeadingOffset = rootHeadingOffset;

            // custom IK settings
            solver.spine.neckStiffness = IKFixes.EntryNeckStiffness.Value;
            solver.spine.bodyRotStiffness = IKFixes.EntryBodyRotStiffness.Value;
            solver.spine.rotateChestByHands = IKFixes.EntryRotateChestByHands.Value;

            if (!IKSystem.vrik.solver.leftLeg.usingKneeTracker)
                IKSystem.vrik.solver.leftLeg.bendToTargetWeight = IKFixes.EntryBendToTargetWeight.Value;
            if (!IKSystem.vrik.solver.rightLeg.usingKneeTracker)
                IKSystem.vrik.solver.rightLeg.bendToTargetWeight = IKFixes.EntryBendToTargetWeight.Value;
        }
        
        int count = IKSystem.Instance.AllTrackingPoints.FindAll(m => m.isActive && m.isValid && m.suggestedRole > TrackingPoint.TrackingRole.Invalid).Count;

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
        CVR_MenuManager.Instance.coreData.core.fullBodyActive = __instance._fbtAvailable = (count >= num && num != 0);
    
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.AssignRemainingTrackers))]
    private static bool Prefix_BodySystem_AssignRemainingTrackers()
    {
        return IKFixes.EntryAssignRemainingTrackers.Value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.MuscleUpdate))]
    private static void Prefix_BodySystem_MuscleUpdate()
    {
        if (BodySystem.isCalibrating)
        {
            IKSystem.Instance.humanPose.bodyRotation = Quaternion.identity;
            //IKSystem.vrik.solver.spine.maxRootAngle = 0f; // idk, testing
        }

        if (BodySystem.isCalibratedAsFullBody && BodySystem.TrackingPositionWeight > 0f)
        {
            bool isRunning = MovementSystem.Instance.movementVector.sqrMagnitude > 0f;
            bool isGrounded = MovementSystem.Instance._isGrounded;
            bool isFlying = MovementSystem.Instance.flying;
            bool playRunningAnimation = BodySystem.PlayRunningAnimationInFullBody;

            if ((playRunningAnimation && (isRunning || !isGrounded && !isFlying)))
            {
                SetPelvisWeight(IKSystem.vrik.solver.spine, 0f);
                IKSystem.Instance.applyOriginalHipPosition = true;
                IKSystem.Instance.applyOriginalHipRotation = true;
            }
            else
            {
                IKSystem.Instance.applyOriginalHipPosition = true;
                IKSystem.Instance.applyOriginalHipRotation = false;
                IKSystem.Instance.humanPose.bodyRotation = Quaternion.identity;
            }
        }

        // TODO: Rewrite to exclude setting T-pose to limbs that are not tracked
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), nameof(BodySystem.Calibrate))]
    private static void Postfix_BodySystem_Calibrate()
    {
        IKSystem.Instance.applyOriginalHipPosition = false;
        IKSystem.Instance.applyOriginalHipRotation = false;
        if (IKSystem.vrik.solver.leftLeg.usingKneeTracker)
        {
            IKSystem.vrik.solver.leftLeg.bendToTargetWeight = 0f;
            IKSystem.vrik.solver.leftLeg.bendGoalWeight = 1f;
        }
        if (IKSystem.vrik.solver.rightLeg.usingKneeTracker)
        {
            IKSystem.vrik.solver.rightLeg.bendToTargetWeight = 0f;
            IKSystem.vrik.solver.rightLeg.bendGoalWeight = 1f;
        }
    }
    
    internal static void OffsetSimulatedRootAngle(float deltaRotation)
    {
        _ikSimulatedRootAngle = Mathf.Repeat(_ikSimulatedRootAngle + deltaRotation, 360f);
    }
}

internal static class IKSystemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(IKSystem), nameof(IKSystem.InitializeAvatar))]
    private static void Prefix_IKSystem_InitializeAvatar(ref IKSystem __instance)
    {
        __instance.applyOriginalHipPosition = true;
        __instance.applyOriginalHipRotation = true;
    }
}

internal static class PlayerSetupPatches
{
    // Last Movement Parent Info
    private static CVRMovementParent lastMovementParent;
    private static Vector3 lastMovementPosition;
    private static Quaternion lastMovementRotation;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ResetIk))]
    private static bool Prefix_PlayerSetup_ResetIk()
    {
        if (IKSystem.vrik == null) 
            return false;

        CVRMovementParent currentParent = MovementSystem.Instance._currentParent;
        if (currentParent == null || currentParent._referencePoint == null) 
            return true;
        
        // Get current position, VR pivots around VR camera
        Vector3 currentPosition = MovementSystem.Instance.rotationPivot.transform.position;
        currentPosition.y = IKSystem.vrik.transform.position.y; // set pivot to floor
        Quaternion currentRotation = Quaternion.Euler(0f, currentParent.transform.rotation.eulerAngles.y, 0f);

        // Convert to delta position (how much changed since last frame)
        Vector3 deltaPosition = currentPosition - lastMovementPosition;
        Quaternion deltaRotation = Quaternion.Inverse(lastMovementRotation) * currentRotation;

        // Prevent targeting previous movement parent
        if (lastMovementParent == currentParent || lastMovementParent == null)
        {
            IKSystem.vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, currentPosition);
            BodySystemPatches.OffsetSimulatedRootAngle(deltaRotation.eulerAngles.y);
        }
        
        lastMovementParent = currentParent;
        lastMovementPosition = currentPosition;
        lastMovementRotation = currentRotation;
        
        return false;
    }
}