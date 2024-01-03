using NAK.AlternateIKSystem.IK.WeightManipulators.BodyParts;
using NAK.AlternateIKSystem.IK.WeightManipulators.Interface;
using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK.WeightManipulators;

public enum BodyPartEnum
{
    Head,
    Pelvis,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Locomotion,
    All
}

public class WeightManipulatorManager
{
    private readonly Dictionary<BodyPartEnum, BodyPart> _bodyParts = new Dictionary<BodyPartEnum, BodyPart>();

    public WeightManipulatorManager()
    {
        _bodyParts.Add(BodyPartEnum.Head, new Head());
        _bodyParts.Add(BodyPartEnum.Pelvis, new Pelvis());
        _bodyParts.Add(BodyPartEnum.LeftArm, new LeftArm());
        _bodyParts.Add(BodyPartEnum.RightArm, new RightArm());
        _bodyParts.Add(BodyPartEnum.LeftLeg, new LeftLeg());
        _bodyParts.Add(BodyPartEnum.RightLeg, new RightLeg());
        _bodyParts.Add(BodyPartEnum.Locomotion, new Locomotion());
    }

    public void SetWeight(BodyPartEnum bodyPartName, float positionWeight, float rotationWeight)
    {
        var bodyPart = _bodyParts[bodyPartName];
        bodyPart.SetPositionWeight(positionWeight);
        bodyPart.SetRotationWeight(rotationWeight);
    }

    public void SetEnabled(BodyPartEnum bodyPartName, bool isEnabled)
    {
        var bodyPart = _bodyParts[bodyPartName];
        bodyPart.SetEnabled(isEnabled);
    }

    public void ApplyWeightsToSolver(IKSolverVR solver)
    {
        foreach (var bodyPart in _bodyParts.Values)
        {
            bodyPart.ApplyWeightToSolver(solver);
        }
    }
}

public class BodyControl
{
    private readonly WeightManipulatorManager _manager;

    public BodyControl(WeightManipulatorManager manager)
    {
        _manager = manager;
    }

    public void SetWeight(string bodyPartName, float positionWeight, float rotationWeight)
    {
        _manager.SetWeight(bodyPartName, positionWeight, rotationWeight);
    }
}

public class DeviceControl
{
    private readonly WeightManipulatorManager _manager;

    public DeviceControl(WeightManipulatorManager manager)
    {
        _manager = manager;
    }

    public void SetEnabled(string bodyPartName, bool isEnabled)
    {
        _manager.SetEnabled(bodyPartName, isEnabled);
    }
}
