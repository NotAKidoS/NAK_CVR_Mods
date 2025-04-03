using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.VRIKHelpers;

public static class VRIKUtils
{
    public static void CalculateInitialIKScaling(VRIK vrik, ref VRIKLocomotionData locomotionData)
    {
        // Get distance between feet and thighs
        float scaleModifier = Mathf.Max(1f, vrik.references.pelvis.lossyScale.x);
        float footDistance = Vector3.Distance(vrik.references.leftFoot.position, vrik.references.rightFoot.position);

        locomotionData.InitialFootDistance = footDistance * 0.5f;
        locomotionData.InitialStepThreshold = footDistance * scaleModifier;
        locomotionData.InitialStepHeight = Vector3.Distance(vrik.references.leftFoot.position, vrik.references.leftCalf.position) * 0.2f;
    }

    public static void CalculateInitialFootsteps(VRIK vrik, ref VRIKLocomotionData locomotionData)
    {
        Transform root = vrik.references.root;
        Transform leftFoot = vrik.references.leftFoot;
        Transform rightFoot = vrik.references.rightFoot;

        // Calculate the world rotation of the root bone at the current frame
        Quaternion rootWorldRot = root.rotation;

        // Calculate the world rotation of the left and right feet relative to the root bone
        locomotionData.InitialFootPosLeft = root.InverseTransformPoint(leftFoot.position);
        locomotionData.InitialFootPosRight = root.InverseTransformPoint(rightFoot.position);
        locomotionData.InitialFootRotLeft = Quaternion.Inverse(rootWorldRot) * leftFoot.rotation;
        locomotionData.InitialFootRotRight = Quaternion.Inverse(rootWorldRot) * rightFoot.rotation;
    }

    public static void ResetToInitialFootsteps(VRIK vrik, VRIKLocomotionData locomotionData, float scaleModifier)
    {
        Transform root = vrik.references.root;
        Quaternion rootWorldRot = vrik.references.root.rotation;

        // hack, use parent transform instead as setting feet position moves root (root.parent), but does not work for VR
        var footsteps = vrik.solver.locomotion.footsteps;
        footsteps[0].Reset(rootWorldRot, root.TransformPoint(locomotionData.InitialFootPosLeft * scaleModifier),
            rootWorldRot * locomotionData.InitialFootRotLeft);
        footsteps[1].Reset(rootWorldRot, root.TransformPoint(locomotionData.InitialFootPosRight * scaleModifier),
            rootWorldRot * locomotionData.InitialFootRotRight);
    }

    public static void ApplyScaleToVRIK(VRIK vrik, VRIKLocomotionData locomotionData, float scaleModifier)
    {
        IKSolverVR.Locomotion locomotionSolver = vrik.solver.locomotion;
        locomotionSolver.footDistance = locomotionData.InitialFootDistance * scaleModifier;
        locomotionSolver.stepThreshold = locomotionData.InitialStepThreshold * scaleModifier;
        ScaleStepHeight(locomotionSolver.stepHeight, locomotionData.InitialStepHeight * scaleModifier);
    }

    private static void ScaleStepHeight(AnimationCurve stepHeightCurve, float mag)
    {
        var keyframes = stepHeightCurve.keys;
        keyframes[1].value = mag;
        stepHeightCurve.keys = keyframes;
    }
}