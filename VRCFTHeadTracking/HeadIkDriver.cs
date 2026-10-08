using ABI_RC.Core.Savior;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.VRCFTHeadTracking;

internal static class HeadIkDriver
{
    private const float RotationRange = 90f;
    private const float PositionRange = 0.5f;

    private static VRIK _vrik;
    private static Transform _savedTarget;

    internal static void Attach(VRIK vrik)
    {
        if (MetaPort.Instance.isUsingVr) 
            return;
        
        _vrik = vrik;
        _savedTarget = null;
        vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdate);
        vrik.onPostSolverUpdate.AddListener(OnPostSolverUpdate);
    }

    private static void OnPreSolverUpdate()
    {
        IKSolverVR.Spine spine = _vrik.solver.spine;
        Transform target = spine.headTarget;
        
        if (!target 
            || !VRCFTHeadTrackingMod.EntryEnabled.Value 
            || !HeadTrackingData.IsFresh) 
            return;

        Vector3 position = target.position;
        Quaternion root = _vrik.transform.rotation;
        Quaternion tracked = Quaternion.Euler(
            HeadTrackingData.Pitch * RotationRange, 
            HeadTrackingData.Yaw * RotationRange, 
            HeadTrackingData.Roll * RotationRange);
        
        if (VRCFTHeadTrackingMod.EntryApplyPosition.Value)
        {
            position += _vrik.transform.TransformDirection(new Vector3(HeadTrackingData.PosX, HeadTrackingData.PosY, HeadTrackingData.PosZ) * PositionRange);
            spine.positionWeight = 1f; // Forced off in Head Bobbing mode otherwise only need apply, game drives every frame)
        }

        _savedTarget = target;
        spine.headTarget = null;
        spine.IKPositionHead = position;
        spine.IKRotationHead = root * tracked * Quaternion.Inverse(root) * target.rotation;
    }

    private static void OnPostSolverUpdate()
    {
        if (!_savedTarget) return;
        _vrik.solver.spine.headTarget = _savedTarget;
        _savedTarget = null;
    }
}