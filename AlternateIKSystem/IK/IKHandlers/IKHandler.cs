using ABI.CCK.Components;
using NAK.AlternateIKSystem.VRIKHelpers;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.IK.IKHandlers;

internal class IKHandler
{
    internal VRIK _vrik;
    internal IKSolverVR _solver;

    // Last Movement Parent Info
    internal Vector3 _movementPosition;
    internal Quaternion _movementRotation;
    internal CVRMovementParent _movementParent;

    #region Virtual Methods

    public virtual void OnInitializeIk()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnPlayerScaled(float scaleDifference, VRIKCalibrationData calibrationData)
    {
    }

    public virtual void OnPlayerHandleMovementParent(CVRMovementParent currentParent)
    {
    }

    #endregion
}