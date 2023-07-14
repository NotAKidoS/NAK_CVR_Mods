using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandlerDesktop : IKHandler
{
    public IKHandlerDesktop(VRIK vrik)
    {
        _vrik = vrik;
        _solver = vrik.solver;
    }

    #region Game Overrides

    public override void OnInitializeIk()
    {
        // Default tracking for Desktop
        shouldTrackHead = true;
        shouldTrackLeftArm = false;
        shouldTrackRightArm = false;
        shouldTrackLeftLeg = false;
        shouldTrackRightLeg = false;
        shouldTrackPelvis = false;
        shouldTrackLocomotion = true;

        _vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdateDesktop);
    }

    #endregion

    #region Weight Overrides

    public override void UpdateWeights()
    {
        // Reset avatar local position
        _vrik.transform.localPosition = Vector3.zero;
        _vrik.transform.localRotation = Quaternion.identity;

        base.UpdateWeights();

        // Desktop should never have head position weight
        _solver.spine.positionWeight = 0f;
    }

    #endregion

    #region VRIK Solver Events

    private void OnPreSolverUpdateDesktop()
    {
        _solver.plantFeet = ModSettings.EntryPlantFeet.Value;

        // Emulate old VRChat hip movement
        if (ModSettings.EntryBodyLeanWeight.Value > 0)
        {
            float weightedAngle = ModSettings.EntryProneThrusting.Value ? 1f : ModSettings.EntryBodyLeanWeight.Value * _solver.locomotion.weight;
            float angle = IKManager.Instance._desktopCamera.localEulerAngles.x;
            angle = angle > 180 ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, _vrik.transform.right);
            _solver.spine.headRotationOffset *= rotation;
        }

        // Make root heading follow within a set limit
        if (ModSettings.EntryBodyHeadingLimit.Value > 0)
        {
            float weightedAngleLimit = ModSettings.EntryBodyHeadingLimit.Value * _solver.locomotion.weight;
            float deltaAngleRoot = Mathf.DeltaAngle(IKManager.Instance.transform.eulerAngles.y, _ikSimulatedRootAngle);
            float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);

            if (absDeltaAngleRoot > weightedAngleLimit)
            {
                deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                _ikSimulatedRootAngle = Mathf.MoveTowardsAngle(_ikSimulatedRootAngle, IKManager.Instance.transform.eulerAngles.y, absDeltaAngleRoot - weightedAngleLimit);
            }

            _solver.spine.rootHeadingOffset = deltaAngleRoot;

            if (ModSettings.EntryPelvisHeadingWeight.Value > 0)
            {
                _solver.spine.pelvisRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * ModSettings.EntryPelvisHeadingWeight.Value, 0f);
                _solver.spine.chestRotationOffset *= Quaternion.Euler(0f, -deltaAngleRoot * ModSettings.EntryPelvisHeadingWeight.Value, 0f);
            }

            if (ModSettings.EntryChestHeadingWeight.Value > 0)
            {
                _solver.spine.chestRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * ModSettings.EntryChestHeadingWeight.Value, 0f);
            }
        }
    }

    #endregion
}