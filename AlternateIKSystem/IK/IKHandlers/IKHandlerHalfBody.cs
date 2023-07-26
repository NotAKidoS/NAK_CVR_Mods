using NAK.AlternateIKSystem.IK.WeightManipulators;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandlerHalfBody : IKHandler
{
    public IKHandlerHalfBody(VRIK vrik)
    {
        _vrik = vrik;
        _solver = vrik.solver;
    }

    #region Game Overrides

    public override void OnInitializeIk()
    {
        // Default tracking for HalfBody
        DeviceControlManipulator.shouldTrackHead = true;
        DeviceControlManipulator.shouldTrackLeftArm = true;
        DeviceControlManipulator.shouldTrackRightArm = true;
        DeviceControlManipulator.shouldTrackLeftLeg = false;
        DeviceControlManipulator.shouldTrackRightLeg = false;
        DeviceControlManipulator.shouldTrackPelvis = false;
        DeviceControlManipulator.shouldTrackLocomotion = true;

        _vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdateHalfBody);
    }

    #endregion

    #region VRIK Solver Events

    private void OnPreSolverUpdateHalfBody()
    {
        _solver.plantFeet = ModSettings.EntryPlantFeet.Value;

        // Make root heading follow within a set limit
        if (ModSettings.EntryBodyHeadingLimit.Value > 0)
        {
            float weightedAngleLimit = ModSettings.EntryBodyHeadingLimit.Value * _solver.locomotion.weight;
            float currentRotation = IKManager.Instance.GetPlayerRotation().y;
            float deltaAngleRoot = Mathf.DeltaAngle(currentRotation, _ikSimulatedRootAngle);

            if (Mathf.Abs(deltaAngleRoot) > weightedAngleLimit)
            {
                deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, currentRotation, Mathf.Abs(deltaAngleRoot) - weightedAngleLimit);
            }

            _solver.spine.rootHeadingOffset = deltaAngleRoot;
        }
    }

    #endregion
}