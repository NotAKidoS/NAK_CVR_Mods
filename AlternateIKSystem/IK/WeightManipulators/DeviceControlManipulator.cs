using NAK.AlternateIKSystem.IK.WeightManipulators.Interface;
using RootMotion.FinalIK;

namespace NAK.AlternateIKSystem.IK.WeightManipulators;

public class DeviceControlManipulator : IWeightManipulator
{
    public static bool shouldTrackAll = true;
    public static bool shouldTrackHead = true;
    public static bool shouldTrackPelvis = true;
    public static bool shouldTrackLeftArm = true;
    public static bool shouldTrackRightArm = true;
    public static bool shouldTrackLeftLeg = true;
    public static bool shouldTrackRightLeg = true;
    public static bool shouldTrackLocomotion = true;

    public WeightManipulatorManager Manager { get; set; }

    // Manipulator for Connected Devices (Has final say)
    public void Update(IKSolverVR solver)
    {
        Manager.TrackAll &= shouldTrackAll;
        Manager.TrackHead &= shouldTrackHead;
        Manager.TrackPelvis &= shouldTrackPelvis;
        Manager.TrackLeftArm &= shouldTrackLeftArm;
        Manager.TrackRightArm &= shouldTrackRightArm;
        Manager.TrackLeftLeg &= shouldTrackLeftLeg;
        Manager.TrackRightLeg &= shouldTrackRightLeg;
        Manager.TrackLocomotion &= shouldTrackLocomotion;
    }
}