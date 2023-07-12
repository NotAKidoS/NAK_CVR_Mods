using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.AlternateIKSystem.VRIKHelpers;

public static class VRIKUtils
{
    public static void CalculateKneeBendNormals(VRIK vrik, ref VRIKCalibrationData calibrationData)
    {
        // Helper function to get position or default to Vector3.zero
        Vector3 GetPositionOrDefault(Transform transform) => transform?.position ?? Vector3.zero;

        // Get assumed left knee normal
        Vector3[] leftVectors = {
            GetPositionOrDefault(vrik.references.leftThigh),
            GetPositionOrDefault(vrik.references.leftCalf),
            GetPositionOrDefault(vrik.references.leftFoot)
        };
        calibrationData.KneeNormalLeft = Quaternion.Inverse(vrik.references.root.rotation) * GetNormalFromArray(leftVectors);

        // Get assumed right knee normal
        Vector3[] rightVectors = {
            GetPositionOrDefault(vrik.references.rightThigh),
            GetPositionOrDefault(vrik.references.rightCalf),
            GetPositionOrDefault(vrik.references.rightFoot)
        };
        calibrationData.KneeNormalRight = Quaternion.Inverse(vrik.references.root.rotation) * GetNormalFromArray(rightVectors);
    }

    public static void ApplyKneeBendNormals(VRIK vrik, VRIKCalibrationData calibrationData)
    {
        // 0 uses bendNormalRelToPelvis, 1 is bendNormalRelToTarget
        // modifying pelvis normal weight is easier math
        vrik.solver.leftLeg.bendToTargetWeight = 0f;
        vrik.solver.rightLeg.bendToTargetWeight = 0f;

        var pelvis_localRotationInverse = Quaternion.Inverse(vrik.references.pelvis.localRotation);
        vrik.solver.leftLeg.bendNormalRelToPelvis = pelvis_localRotationInverse * calibrationData.KneeNormalLeft;
        vrik.solver.rightLeg.bendNormalRelToPelvis = pelvis_localRotationInverse * calibrationData.KneeNormalRight;
    }

    private static Vector3 GetNormalFromArray(Vector3[] positions)
    {
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < positions.Length; i++)
        {
            centroid += positions[i];
        }
        centroid /= positions.Length;

        Vector3 normal = Vector3.zero;
        for (int i = 0; i < positions.Length - 2; i++)
        {
            Vector3 side1 = positions[i] - centroid;
            Vector3 side2 = positions[i + 1] - centroid;
            normal += Vector3.Cross(side1, side2);
        }
        return normal.normalized;
    }

    public static void CalculateInitialIKScaling(VRIK vrik, ref VRIKCalibrationData calibrationData)
    {
        // Get distance between feet and thighs
        float scaleModifier = Mathf.Max(1f, vrik.references.pelvis.lossyScale.x);
        float footDistance = Vector3.Distance(vrik.references.leftFoot.position, vrik.references.rightFoot.position);

        calibrationData.InitialFootDistance = footDistance * 0.5f;
        calibrationData.InitialStepThreshold = footDistance * scaleModifier;
        calibrationData.InitialStepHeight = Vector3.Distance(vrik.references.leftFoot.position, vrik.references.leftCalf.position) * 0.2f;
        calibrationData.InitialHeadHeight = Mathf.Abs(vrik.references.head.position.y - vrik.references.rightFoot.position.y);
    }

    public static void CalculateInitialFootsteps(VRIK vrik, ref VRIKCalibrationData calibrationData)
    {
        Transform root = vrik.references.root;
        Transform leftFoot = vrik.references.leftFoot;
        Transform rightFoot = vrik.references.rightFoot;

        // Calculate the world rotation of the root bone at the current frame
        Quaternion rootWorldRot = root.rotation;

        // Calculate the world rotation of the left and right feet relative to the root bone
        calibrationData.InitialFootPosLeft = root.parent.InverseTransformPoint(leftFoot.position);
        calibrationData.InitialFootPosRight = root.parent.InverseTransformPoint(rightFoot.position);
        calibrationData.InitialFootRotLeft = Quaternion.Inverse(rootWorldRot) * leftFoot.rotation;
        calibrationData.InitialFootRotRight = Quaternion.Inverse(rootWorldRot) * rightFoot.rotation;
    }

    public static void ResetToInitialFootsteps(VRIK vrik, VRIKCalibrationData calibrationData, float scaleModifier)
    {
        var locomotionSolver = vrik.solver.locomotion;

        var footsteps = locomotionSolver.footsteps;
        var footstepLeft = footsteps[0];
        var footstepRight = footsteps[1];

        var root = vrik.references.root;
        var rootWorldRot = vrik.references.root.rotation;

        // hack, use parent transform instead as setting feet position moves root
        footstepLeft.Reset(rootWorldRot, root.parent.TransformPoint(calibrationData.InitialFootPosLeft * scaleModifier), rootWorldRot * calibrationData.InitialFootRotLeft);
        footstepRight.Reset(rootWorldRot, root.parent.TransformPoint(calibrationData.InitialFootPosRight * scaleModifier), rootWorldRot * calibrationData.InitialFootRotRight);
    }

    public static void ApplyScaleToVRIK(VRIK vrik, VRIKCalibrationData calibrationData, float scaleModifier)
    {
        var locomotionSolver = vrik.solver.locomotion;
        locomotionSolver.footDistance = calibrationData.InitialFootDistance * scaleModifier;
        locomotionSolver.stepThreshold = calibrationData.InitialStepThreshold * scaleModifier;
        ScaleStepHeight(locomotionSolver.stepHeight, calibrationData.InitialStepHeight * scaleModifier);
    }

    static void ScaleStepHeight(AnimationCurve stepHeightCurve, float mag)
    {
        Keyframe[] keyframes = stepHeightCurve.keys;
        keyframes[1].value = mag;
        stepHeightCurve.keys = keyframes;
    }

    public static void InitiateVRIKSolver(VRIK vrik)
    {
        vrik.solver.SetToReferences(vrik.references);
        vrik.solver.Initiate(vrik.transform);
    }
}