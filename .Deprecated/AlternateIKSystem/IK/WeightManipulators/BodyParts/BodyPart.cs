using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK.WeightManipulators.BodyParts;

public abstract class BodyPart
{
    protected float positionWeight = 1f;
    protected float rotationWeight = 1f;
    protected bool isEnabled = true;

    public void SetPositionWeight(float weight)
    {
        this.positionWeight *= weight;
    }

    public void SetRotationWeight(float weight)
    {
        this.rotationWeight *= weight;
    }

    public void SetEnabled(bool isEnabled)
    {
        this.isEnabled = isEnabled;
    }

    public abstract void ApplyWeightToSolver(IKSolverVR solver);
}

public class Head : BodyPart
{
    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;
        solver.spine.positionWeight *= positionWeight;
        solver.spine.rotationWeight *= rotationWeight;
    }
}

public class Pelvis : BodyPart
{
    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;
        solver.spine.pelvisPositionWeight *= positionWeight;
        solver.spine.pelvisRotationWeight *= rotationWeight;
    }
}

public class LeftArm : BodyPart
{
    private float bendGoalWeight = 1f;

    public void SetBendGoalWeight(float weight)
    {
        this.bendGoalWeight *= weight;
    }

    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;

        solver.leftArm.positionWeight *= positionWeight;
        solver.leftArm.rotationWeight *= rotationWeight;
        solver.leftArm.bendGoalWeight *= bendGoalWeight;
    }
}

public class RightArm : BodyPart
{
    private float bendGoalWeight = 1f;

    public void SetBendGoalWeight(float weight)
    {
        this.bendGoalWeight *= weight;
    }

    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;

        solver.rightArm.positionWeight *= positionWeight;
        solver.rightArm.rotationWeight *= rotationWeight;
        solver.rightArm.bendGoalWeight *= bendGoalWeight;
    }
}

public class LeftLeg : BodyPart
{
    private float bendGoalWeight = 1f;

    public void SetBendGoalWeight(float weight)
    {
        this.bendGoalWeight *= weight;
    }

    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;

        solver.leftLeg.positionWeight *= positionWeight;
        solver.leftLeg.rotationWeight *= rotationWeight;
        solver.leftLeg.bendGoalWeight *= bendGoalWeight;
    }
}

public class RightLeg : BodyPart
{
    private float bendGoalWeight = 1f;

    public void SetBendGoalWeight(float weight)
    {
        this.bendGoalWeight *= weight;
    }

    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;

        solver.rightLeg.positionWeight *= positionWeight;
        solver.rightLeg.rotationWeight *= rotationWeight;
        solver.rightLeg.bendGoalWeight *= bendGoalWeight;
    }
}

public class Locomotion : BodyPart
{
    public override void ApplyWeightToSolver(IKSolverVR solver)
    {
        if (!isEnabled) return;
        solver.locomotion.weight *= positionWeight;
    }
}
