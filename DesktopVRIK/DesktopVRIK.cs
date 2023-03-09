using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;
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

    // DesktopVRIK References
    bool _isEmotePlaying;
    float _simulatedRootAngle;
    Transform _avatarTransform;
    Transform _cameraTransform;

    // IK Stuff
    VRIK _vrik;
    LookAtIK _lookAtIK;
    IKSolverVR _ikSolver;

    // Movement System Stuff
    MovementSystem movementSystem;
    Traverse _isGroundedTraverse;

    // Movement Parent Stuff
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Traverse _currentParentTraverse;
    static readonly FieldInfo _referencePointField = typeof(CVRMovementParent).GetField("_referencePoint", BindingFlags.NonPublic | BindingFlags.Instance);

    void Start()
    {
        Instance = this;
        Calibrator = new DesktopVRIKCalibrator();
        DesktopVRIKMod.UpdateAllSettings();

        movementSystem = GetComponent<MovementSystem>();
        _cameraTransform = PlayerSetup.Instance.desktopCamera.transform;
        _isGroundedTraverse = Traverse.Create(movementSystem).Field("_isGrounded");
        _currentParentTraverse = Traverse.Create(movementSystem).Field("_currentParent");
    }

    public void OnSetupAvatarDesktop()
    {
        if (!Setting_Enabled) return;
        Calibrator.CalibrateDesktopVRIK();

        _vrik = Calibrator.vrik;
        _lookAtIK = Calibrator.lookAtIK;
        _ikSolver = Calibrator.vrik.solver;
        _avatarTransform = Calibrator.avatarTransform;

        _simulatedRootAngle = transform.eulerAngles.y;
    }

    public bool OnSetupIKScaling(float scaleDifference)
    {
        if (_vrik == null) return false;

        VRIKUtils.ApplyScaleToVRIK
        (
            _vrik,
            Calibrator.initialFootDistance,
            Calibrator.initialStepThreshold,
            Calibrator.initialStepHeight,
            scaleDifference
        );

        _ikSolver?.Reset();
        ResetDesktopVRIK();
        return true;
    }

    public void OnPlayerSetupUpdate(bool isEmotePlaying)
    {
        bool changed = isEmotePlaying != _isEmotePlaying;
        if (!changed) return;

        _isEmotePlaying = isEmotePlaying;

        _avatarTransform.localPosition = Vector3.zero;
        _avatarTransform.localRotation = Quaternion.identity;

        if (_lookAtIK != null)
            _lookAtIK.enabled = !isEmotePlaying;

        BodySystem.TrackingEnabled = !isEmotePlaying;

        _ikSolver?.Reset();
        ResetDesktopVRIK();
    }

    public bool OnPlayerSetupResetIk()
    {
        if (_vrik == null) return false;

        CVRMovementParent currentParent = _currentParentTraverse.GetValue<CVRMovementParent>();
        if (currentParent == null) return false;

        Transform referencePoint = (Transform)_referencePointField.GetValue(currentParent);
        if (referencePoint == null) return false;

        var currentPosition = referencePoint.position;
        var currentRotation = currentParent.transform.rotation;

        // Keep only the Y-axis rotation
        currentRotation = Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);
        
        var deltaPosition = currentPosition - _previousPosition;
        var deltaRotation = Quaternion.Inverse(_previousRotation) * currentRotation;

        var platformPivot = transform.position;
        _ikSolver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);

        _previousPosition = currentPosition;
        _previousRotation = currentRotation;

        ResetDesktopVRIK();
        return true;
    }


    public void ResetDesktopVRIK()
    {
        _simulatedRootAngle = transform.eulerAngles.y;
    }

    public void OnPreSolverUpdate()
    {
        if (_isEmotePlaying) return;

        bool isGrounded = _isGroundedTraverse.GetValue<bool>();

        // Calculate weight
        float weight = _ikSolver.IKPositionWeight;
        weight *= 1f - movementSystem.movementVector.magnitude;
        weight *= isGrounded ? 1f : 0f;

        // Reset avatar offset
        _avatarTransform.localPosition = Vector3.zero;
        _avatarTransform.localRotation = Quaternion.identity;

        // Set plant feet
        _ikSolver.plantFeet = Setting_PlantFeet;

        // Emulate old VRChat hip movement
        if (Setting_BodyLeanWeight > 0)
        {
            float weightedAngle = Setting_BodyLeanWeight * weight;
            float angle = _cameraTransform.localEulerAngles.x;
            angle = angle > 180 ? angle - 360 : angle;
            Quaternion rotation = Quaternion.AngleAxis(angle * weightedAngle, _avatarTransform.right);
            _ikSolver.spine.headRotationOffset *= rotation;
        }

        // Make root heading follow within a set limit
        if (Setting_BodyHeadingLimit > 0)
        {
            float weightedAngleLimit = Setting_BodyHeadingLimit * weight;
            float deltaAngleRoot = Mathf.DeltaAngle(transform.eulerAngles.y, _simulatedRootAngle);
            float absDeltaAngleRoot = Mathf.Abs(deltaAngleRoot);
            if (absDeltaAngleRoot > weightedAngleLimit)
            {
                deltaAngleRoot = Mathf.Sign(deltaAngleRoot) * weightedAngleLimit;
                _simulatedRootAngle = Mathf.MoveTowardsAngle(_simulatedRootAngle, transform.eulerAngles.y, absDeltaAngleRoot - weightedAngleLimit);
            }
            _ikSolver.spine.rootHeadingOffset = deltaAngleRoot;
            if (Setting_PelvisHeadingWeight > 0)
            {
                _ikSolver.spine.pelvisRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * Setting_PelvisHeadingWeight, 0f);
                _ikSolver.spine.chestRotationOffset *= Quaternion.Euler(0f, -deltaAngleRoot * Setting_PelvisHeadingWeight, 0f);
            }
            if (Setting_ChestHeadingWeight > 0)
            {
                _ikSolver.spine.chestRotationOffset *= Quaternion.Euler(0f, deltaAngleRoot * Setting_ChestHeadingWeight, 0f);
            }
        }
    }
}