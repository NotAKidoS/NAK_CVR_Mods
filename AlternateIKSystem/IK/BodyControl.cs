using ABI_RC.Core.Player;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK;

public class BodyControl
{
    #region Player Settings

    public static bool useHipTracking = true;
    public static bool useChestTracking = true;
    public static bool useLeftFootTracking = true;
    public static bool useRightFootTracking = true;

    public static bool useLeftElbowTracking = false;
    public static bool useRightElbowTracking = false;
    public static bool useLeftKneeTracking = false;
    public static bool useRightKneeTracking = false;

    public static bool useLocomotionAnimations = true;

    #endregion

    #region Tracking Controls

    public static bool TrackingAll = true;
    public static bool TrackingHead = true;
    public static bool TrackingPelvis = true;
    public static bool TrackingLeftArm = true;
    public static bool TrackingRightArm = true;
    public static bool TrackingLeftLeg = true;
    public static bool TrackingRightLeg = true;
    public static bool TrackingLocomotion = true;
    public static float TrackingPositionWeight = 1f;

    // TODO: decide if this is considered "Tracking Controls"
    public static float TrackingMaxRootAngle = 0f;

    #endregion

    #region Avatar Info

    public static float AvatarUpright = 1f;

    #endregion

    #region BodyControl Configuration

    public static float InvalidTrackerDistance = 1f;

    #endregion

    public void Update()
    {
        TrackingAll = ShouldTrackAll();
        TrackingLocomotion = ShouldTrackLocomotion();
        AvatarUpright = GetPlayerUpright();
    }

    #region Private Methods

    private static bool ShouldTrackAll()
    {
        return !PlayerSetup.Instance._emotePlaying;
    }

    private static bool ShouldTrackLocomotion()
    {
        return !(MovementSystem.Instance.movementVector.magnitude > 0f
                 || MovementSystem.Instance.crouching
                 || MovementSystem.Instance.prone
                 || MovementSystem.Instance.flying
                 || MovementSystem.Instance.sitting
                 || !MovementSystem.Instance._isGrounded);
    }

    private static float GetPlayerUpright()
    {
        float avatarHeight = PlayerSetup.Instance._avatarHeight;
        float currentHeight = PlayerSetup.Instance.GetViewRelativePosition().y;
        return Mathf.Clamp01((avatarHeight > 0f) ? (currentHeight / avatarHeight) : 0f);
    }

    #endregion

    #region Solver Weight Helpers

    public static void SetHeadWeight(IKSolverVR.Spine spine, LookAtIK lookAtIk, float weight)
    {
        spine.positionWeight = weight;
        spine.rotationWeight = weight;
        if (lookAtIk != null)
            lookAtIk.solver.IKPositionWeight = weight;
    }

    public static void SetArmWeight(IKSolverVR.Arm arm, float weight)
    {
        arm.positionWeight = weight;
        arm.rotationWeight = weight;
        arm.shoulderRotationWeight = weight;
        arm.shoulderTwistWeight = weight;
        arm.bendGoalWeight = arm.bendGoal != null ? weight : 0f;
    }

    public static void SetLegWeight(IKSolverVR.Leg leg, float weight)
    {
        leg.positionWeight = weight;
        leg.rotationWeight = weight;
        leg.bendGoalWeight = leg.usingKneeTracker ? weight : 0f;
    }

    public static void SetPelvisWeight(IKSolverVR.Spine spine, float weight)
    {
        spine.pelvisPositionWeight = weight;
        spine.pelvisRotationWeight = weight;
    }

    #endregion
}