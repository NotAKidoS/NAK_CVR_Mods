using NAK.AlternateIKSystem.IK.WeightManipulators.Interface;
using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK.WeightManipulators;

public class TrackingControlManipulator : IWeightManipulator
{
    public WeightManipulatorManager Manager { get; set; }

    // Manipulator for External Control (Auto, State Behaviour)
    public void Update(IKSolverVR solver)
    {
        Manager.TrackAll |= BodyControl.TrackingAll;
        Manager.TrackHead |= BodyControl.TrackingHead;
        Manager.TrackPelvis |= BodyControl.TrackingPelvis;
        Manager.TrackLeftArm |= BodyControl.TrackingLeftArm;
        Manager.TrackRightArm |= BodyControl.TrackingRightArm;
        Manager.TrackLeftLeg |= BodyControl.TrackingLeftLeg;
        Manager.TrackRightLeg |= BodyControl.TrackingRightLeg;
        Manager.TrackLocomotion |= BodyControl.TrackingLocomotion;
    }
}