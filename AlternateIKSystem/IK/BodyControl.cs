using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK;

public static class BodyControl
{
    #region Tracking Controls

    public static bool TrackingAll = true;
    public static bool TrackingHead = true;
    public static bool TrackingPelvis = true;
    public static bool TrackingLeftArm = true;
    public static bool TrackingRightArm = true;
    public static bool TrackingLeftLeg = true;
    public static bool TrackingRightLeg = true;

    //TODO: dont do this, it is effective but lazy
    public static bool TrackingLocomotion
    {
        get => _trackingLocomotion;
        set
        {
            if (_trackingLocomotion == value)
                return;

            _trackingLocomotion = value;
            IKManager.solver?.Reset();
        }
    }
    private static bool _trackingLocomotion = true;

    public static float TrackingPositionWeight = 1f;

    // TODO: decide if these are considered "Tracking Controls"
    public static float TrackingUpright = 1f;
    public static float TrackingMaxRootAngle = 0f;

    #endregion

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

    #region BodyControl Configuration

    public static float InvalidTrackerDistance = 1f;

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