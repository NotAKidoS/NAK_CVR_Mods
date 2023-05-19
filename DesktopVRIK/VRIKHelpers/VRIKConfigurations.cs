namespace NAK.DesktopVRIK.VRIKHelper;

public static class VRIKConfigurations
{
    public static VRIKConfiguration DesktopVRIKConfiguration()
    {
        return new VRIKConfiguration
        {
            // Solver settings
            LocomotionWeight = 0f,
            LocomotionAngleThreshold = 30f,
            LocomotionMaxLegStretch = 1f,
            SpineMinHeadHeight = 0f,
            SolverIKPositionWeight = 1f,
            SpineChestClampWeight = 0f,
            SpineMaintainPelvisPosition = 1f,

            // Body leaning settings
            SpineBodyPosStiffness = 1f,
            SpineBodyRotStiffness = 0.2f,
            SpineNeckStiffness = 0.0001f, //hack

            // Locomotion settings
            LocomotionVelocityFactor = 0f,
            LocomotionMaxVelocity = 0f,
            LocomotionRootSpeed = 1000f,

            // Chest rotation
            SpineRotateChestByHands = 0f, //pam, bid, leap motion change

            // LookAtIK priority
            SpineHeadClampWeight = 0.2f,

            // Tippytoes
            SpinePositionWeight = 0f,
            SpineRotationWeight = 1f,

            // Emotes
            SpineMaxRootAngle = 180f,

            // BodySystem
            SpinePelvisPositionWeight = 0f,
            LeftArmPositionWeight = 0f,
            LeftArmRotationWeight = 0f,
            RightArmPositionWeight = 0f,
            RightArmRotationWeight = 0f,
            LeftLegPositionWeight = 0f,
            LeftLegRotationWeight = 0f,
            RightLegPositionWeight = 0f,
            RightLegRotationWeight = 0f,
        };
    }
}