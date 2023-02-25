using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
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
        Setting_HipMovement = true,
        Setting_ResetOnLand = true,
        Setting_PlantFeet = true,
        Setting_EnforceViewPosition;
    public float
        Setting_BodyLeanWeight,
        Setting_BodyHeadingLimit,
        Setting_PelvisHeadingWeight,
        Setting_ChestHeadingWeight;

    // Internal Stuff
    private float
        ik_SimulatedRootAngle;
    private bool
        ms_lastGrounded,
        ps_emoteIsPlaying;
    static readonly FieldInfo ms_isGrounded = typeof(MovementSystem).GetField("_isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);

    void Start()
    {
        Calibrator = new DesktopVRIKCalibrator();
        Instance = this;
        DesktopVRIKMod.UpdateAllSettings();
    }

    public void OnSetupAvatarDesktop()
    {
        if (!Setting_Enabled) return;
        Calibrator.SetupDesktopVRIK();
        ik_SimulatedRootAngle = transform.eulerAngles.y;
    }

    //public void OnReCalibrateAvatar()
    //{
    //    Calibrator.RecalibrateDesktopVRIK();
    //    ik_SimulatedRootAngle = transform.eulerAngles.y;
    //}

    public bool OnApplyAvatarScaleToIk(float height)
    {
        if (Calibrator.vrik != null)
        {
            Calibrator.vrik.solver.locomotion.footDistance = Calibrator.initialFootDistance * height;
            Calibrator.vrik.solver.locomotion.stepThreshold = Calibrator.initialStepThreshold * height;
            return true;
        }
        return false;
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        bool changed = isEmotePlaying != ps_emoteIsPlaying;
        if (changed)
        {
            ps_emoteIsPlaying = isEmotePlaying;
            Calibrator.vrik.transform.localPosition = Vector3.zero;
            Calibrator.vrik.transform.localRotation = Quaternion.identity;
            if (Calibrator.lookAtIK != null)
            {
                Calibrator.lookAtIK.enabled = !isEmotePlaying;
            }
            BodySystem.TrackingEnabled = !isEmotePlaying;
            Calibrator.vrik.solver?.Reset();
        }
    }

    public void OnPreSolverUpdate()
    {
        if (ps_emoteIsPlaying) return;

        bool isGrounded = (bool)ms_isGrounded.GetValue(MovementSystem.Instance);

        // Calculate everything that affects weight
        float weight = Calibrator.vrik.solver.IKPositionWeight;
        weight *= (1 - MovementSystem.Instance.movementVector.magnitude);
        weight *= isGrounded ? 1f : 0f;

        // Reset avatar offset (VRIK will literally make you walk away from root otherwise)
        Calibrator.vrik.transform.localPosition = Vector3.zero;
        Calibrator.vrik.transform.localRotation = Quaternion.identity;

        // Plant feet is nice for Desktop
        Calibrator.vrik.solver.plantFeet = Setting_PlantFeet;

        // This is nice for walk cycles
        //Calibrator.vrik.solver.spine.rotateChestByHands = Setting_RotateChestByHands * weight;

        //reset solver if weight changes dramatically
        if (Setting_ResetOnLand)
        {
            if (isGrounded && !ms_lastGrounded)
            {
                Calibrator.vrik.solver.Reset();
            }
            ms_lastGrounded = isGrounded;
        }

        // Old VRChat hip movement emulation
        if (Setting_BodyLeanWeight > 0)
        {
            float weightedAngle = Setting_BodyLeanWeight * weight;
            float angle = PlayerSetup.Instance.desktopCamera.transform.localEulerAngles.x;
            angle = (angle > 180) ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, IKSystem.Instance.avatar.transform.right);
            Calibrator.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Head, rotation);
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
            Calibrator.vrik.solver.spine.rootHeadingOffset = currentAngle;
            if (Setting_PelvisHeadingWeight > 0)
            {
                Calibrator.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Pelvis, new Vector3(0f, currentAngle * Setting_PelvisHeadingWeight, 0f));
                Calibrator.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Chest, new Vector3(0f, -currentAngle * Setting_PelvisHeadingWeight, 0f));
            }
            if (Setting_ChestHeadingWeight > 0)
            {
                Calibrator.vrik.solver.AddRotationOffset(IKSolverVR.RotationOffset.Chest, new Vector3(0f, currentAngle * Setting_ChestHeadingWeight, 0f));
            }
        }
    }
}