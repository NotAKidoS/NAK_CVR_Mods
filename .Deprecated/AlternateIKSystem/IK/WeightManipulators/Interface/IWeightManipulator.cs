using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK.WeightManipulators.Interface;

public interface IWeightManipulator
{
    WeightManipulatorManager Manager { get; set; }
    void Update(IKSolverVR solver);
}