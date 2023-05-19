namespace NAK.DesktopVRIK.VRIKHelper;

public class VRIKConfiguration
{
    // Solver settings
    public float LocomotionWeight { get; set; }
    public float LocomotionAngleThreshold { get; set; }
    public float LocomotionMaxLegStretch { get; set; }
    public float SpineMinHeadHeight { get; set; }
    public float SolverIKPositionWeight { get; set; }
    public float SpineChestClampWeight { get; set; }
    public float SpineMaintainPelvisPosition { get; set; }

    // Body leaning settings
    public float SpineBodyPosStiffness { get; set; }
    public float SpineBodyRotStiffness { get; set; }
    public float SpineNeckStiffness { get; set; }

    // Locomotion settings
    public float LocomotionVelocityFactor { get; set; }
    public float LocomotionMaxVelocity { get; set; }
    public float LocomotionRootSpeed { get; set; }

    // Chest rotation
    public float SpineRotateChestByHands { get; set; }

    public float SpineHeadClampWeight { get; set; }

    public float SpinePositionWeight { get; set; }
    public float SpineRotationWeight { get; set; }

    public float SpineMaxRootAngle { get; set; }

    // BodySystem
    public float SpinePelvisPositionWeight { get; set; }
    public float LeftArmPositionWeight { get; set; }
    public float LeftArmRotationWeight { get; set; }
    public float RightArmPositionWeight { get; set; }
    public float RightArmRotationWeight { get; set; }
    public float LeftLegPositionWeight { get; set; }
    public float LeftLegRotationWeight { get; set; }
    public float RightLegPositionWeight { get; set; }
    public float RightLegRotationWeight { get; set; }
}

public static class VRIKConfigurator
{
    public static void ApplyVRIKConfiguration(CachedSolver cachedSolver, VRIKConfiguration config)
    {
        cachedSolver.Solver.IKPositionWeight = config.SolverIKPositionWeight;
        cachedSolver.Locomotion.weight = config.LocomotionWeight;
        cachedSolver.Locomotion.angleThreshold = config.LocomotionAngleThreshold;
        cachedSolver.Locomotion.maxLegStretch = config.LocomotionMaxLegStretch;

        cachedSolver.Spine.chestClampWeight = config.SpineChestClampWeight;
        cachedSolver.Spine.maintainPelvisPosition = config.SpineMaintainPelvisPosition;
        cachedSolver.Spine.minHeadHeight = config.SpineMinHeadHeight;

        cachedSolver.Spine.bodyPosStiffness = config.SpineBodyPosStiffness;
        cachedSolver.Spine.bodyRotStiffness = config.SpineBodyRotStiffness;
        cachedSolver.Spine.neckStiffness = config.SpineNeckStiffness;

        cachedSolver.Locomotion.velocityFactor = config.LocomotionVelocityFactor;
        cachedSolver.Locomotion.maxVelocity = config.LocomotionMaxVelocity;
        cachedSolver.Locomotion.rootSpeed = config.LocomotionRootSpeed;

        cachedSolver.Spine.rotateChestByHands = config.SpineRotateChestByHands;

        cachedSolver.Spine.headClampWeight = config.SpineHeadClampWeight;

        cachedSolver.Spine.positionWeight = config.SpinePositionWeight;
        cachedSolver.Spine.rotationWeight = config.SpineRotationWeight;

        cachedSolver.Spine.maxRootAngle = config.SpineMaxRootAngle;

        cachedSolver.Spine.maintainPelvisPosition = config.SpineMaintainPelvisPosition;
        cachedSolver.Spine.pelvisPositionWeight = config.SpinePelvisPositionWeight;
        cachedSolver.LeftArm.positionWeight = config.LeftArmPositionWeight;
        cachedSolver.LeftArm.rotationWeight = config.LeftArmRotationWeight;
        cachedSolver.RightArm.positionWeight = config.RightArmPositionWeight;
        cachedSolver.RightArm.rotationWeight = config.RightArmRotationWeight;
        cachedSolver.LeftLeg.positionWeight = config.LeftLegPositionWeight;
        cachedSolver.LeftLeg.rotationWeight = config.LeftLegRotationWeight;
        cachedSolver.RightLeg.positionWeight = config.RightLegPositionWeight;
        cachedSolver.RightLeg.rotationWeight = config.RightLegRotationWeight;
    }
}