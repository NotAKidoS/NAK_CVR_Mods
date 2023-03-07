using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.DesktopVRIK;

public class DesktopVRIK : MonoBehaviour
{
    public static DesktopVRIK Instance;
    public DesktopVRIKCalibrator Calibrator;

    // DesktopVRIK Settings
    public bool
        Setting_Enabled = true,
        Setting_PlantFeet = true;
    public float
        Setting_BodyLeanWeight,
        Setting_BodyHeadingLimit,
        Setting_PelvisHeadingWeight,
        Setting_ChestHeadingWeight;

    // Internal Stuff
    bool ps_emoteIsPlaying;
    float ik_SimulatedRootAngle;
    Transform desktopCameraTransform;
    static readonly FieldInfo ms_isGrounded = typeof(MovementSystem).GetField("_isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);

    void Start()
    {
        desktopCameraTransform = PlayerSetup.Instance.desktopCamera.transform;
        Calibrator = new DesktopVRIKCalibrator();
        Instance = this;
        DesktopVRIKMod.UpdateAllSettings();
    }

    public void OnSetupAvatarDesktop()
    {
        if (!Setting_Enabled) return;
        Calibrator.CalibrateDesktopVRIK();
        ResetDesktopVRIK();
    }

    public bool OnSetupIKScaling(float scaleDifference)
    {
        if (Calibrator.vrik == null) return false;

        VRIKUtils.ApplyScaleToVRIK
        (
            Calibrator.vrik,
            Calibrator.initialFootDistance,
            Calibrator.initialStepThreshold,
            Calibrator.initialStepHeight,
            scaleDifference
        );

        ResetDesktopVRIK();
        return true;
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        bool changed = isEmotePlaying != ps_emoteIsPlaying;
        if (!changed) return;

        ps_emoteIsPlaying = isEmotePlaying;

        Calibrator.avatarTransform.localPosition = Vector3.zero;
        Calibrator.avatarTransform.localRotation = Quaternion.identity;

        if (Calibrator.lookAtIK != null)
            Calibrator.lookAtIK.enabled = !isEmotePlaying;

        BodySystem.TrackingEnabled = !isEmotePlaying;

        Calibrator.vrik.solver?.Reset();
        ResetDesktopVRIK();
    }


    public void ResetDesktopVRIK()
    {
        ik_SimulatedRootAngle = transform.eulerAngles.y;
    }

    public void OnPreSolverUpdate()
    {
        if (ps_emoteIsPlaying) return;

        var movementSystem = MovementSystem.Instance;
        var vrikSolver = Calibrator.vrik.solver;
        var avatarTransform = Calibrator.avatarTransform;

        bool isGrounded = (bool)ms_isGrounded.GetValue(movementSystem);

        // Calculate weight
        float weight = vrikSolver.IKPositionWeight;
        weight *= 1f - movementSystem.movementVector.magnitude;
        weight *= isGrounded ? 1f : 0f;

        // Reset avatar offset
        avatarTransform.localPosition = Vector3.zero;
        avatarTransform.localRotation = Quaternion.identity;

        // Set plant feet
        vrikSolver.plantFeet = Setting_PlantFeet;

        // Emulate old VRChat hip movement
        if (Setting_BodyLeanWeight > 0)
        {
            float weightedAngle = Setting_BodyLeanWeight * weight;
            float angle = desktopCameraTransform.localEulerAngles.x;
            angle = angle > 180 ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, avatarTransform.right);
            vrikSolver.AddRotationOffset(IKSolverVR.RotationOffset.Head, rotation);
        }

        // Make root heading follow within a set limit
        if (Setting_BodyHeadingLimit > 0)
        {
            float weightedAngleLimit = Setting_BodyHeadingLimit * weight;
            float currentAngle = Mathf.DeltaAngle(transform.eulerAngles.y, ik_SimulatedRootAngle);
            float angleMaxDelta = Mathf.Abs(currentAngle);
            if (angleMaxDelta > weightedAngleLimit)
            {
                currentAngle = Mathf.Sign(currentAngle) * weightedAngleLimit;
                ik_SimulatedRootAngle = Mathf.MoveTowardsAngle(ik_SimulatedRootAngle, transform.eulerAngles.y, angleMaxDelta - weightedAngleLimit);
            }
            vrikSolver.spine.rootHeadingOffset = currentAngle;
            if (Setting_PelvisHeadingWeight > 0)
            {
                vrikSolver.AddRotationOffset(IKSolverVR.RotationOffset.Pelvis, new Vector3(0f, currentAngle * Setting_PelvisHeadingWeight, 0f));
                vrikSolver.AddRotationOffset(IKSolverVR.RotationOffset.Chest, new Vector3(0f, -currentAngle * Setting_PelvisHeadingWeight, 0f));
            }
            if (Setting_ChestHeadingWeight > 0)
            {
                vrikSolver.AddRotationOffset(IKSolverVR.RotationOffset.Chest, new Vector3(0f, currentAngle * Setting_ChestHeadingWeight, 0f));
            }
        }
    }
}