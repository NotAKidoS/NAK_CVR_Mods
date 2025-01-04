using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

// TODO:
// Fix multi-grab (limitation of Pickupable)
//   Think this can be fixed by forcing ungrab & monitoring ourselves for release
// Add configurable override for steering range
// Fix steering wheel resetting immediatly on release
// Fix input patch not being multiplicative (so joysticks can still work)
// Prevent pickup in Desktop

public class SteeringWheelPickup : Pickupable
{
    private RCC_CarControllerV3 _carController;

    private static readonly Dictionary<RCC_CarControllerV3, SteeringWheelPickup> ActiveWheels = new();

    public static float GetSteerInput(RCC_CarControllerV3 carController)
    {
        if (ActiveWheels.TryGetValue(carController, out SteeringWheelPickup wheel) && wheel.IsPickedUp)
            return wheel.GetNormalizedValue();
        return 0f;
    }

    public void SetupSteeringWheel(RCC_CarControllerV3 carController)
    {
        _carController = carController;
        if (!ActiveWheels.ContainsKey(carController)) 
        {
            ActiveWheels[carController] = this;
            carController.useCounterSteering = false;
            carController.useSteeringSmoother = false;
        }
    }

    private void OnDestroy()
    {
        if (_carController != null && ActiveWheels.ContainsKey(_carController))
            ActiveWheels.Remove(_carController);
    }

    #region Configuration Properties Override

    public override bool DisallowTheft => true;
    public override float MaxGrabDistance => 0.8f;
    public override float MaxPushDistance => 0f;
    public override bool IsAutoHold => false;
    public override bool IsObjectRotationAllowed => false;
    public override bool IsObjectPushPullAllowed => false;
    public override bool IsObjectUseAllowed => false;

    public override bool CanPickup => IsPickupable && _carController?.SteeringWheel != null;

    #endregion Configuration Properties Override
    
    #region RCC Stuff
    
    private float GetMaxSteeringRange()
        => _carController.steerAngle * Mathf.Abs(_carController.steeringWheelAngleMultiplier);

    private float GetSteeringWheelSign() 
        => Mathf.Sign(_carController.steeringWheelAngleMultiplier) * -1f; // Idk
    
    private Vector3 GetSteeringWheelLocalAxis()
    {
        return _carController.steeringWheelRotateAround switch
        {
            RCC_CarControllerV3.SteeringWheelRotateAround.XAxis => Vector3.right,
            RCC_CarControllerV3.SteeringWheelRotateAround.YAxis => Vector3.up,
            RCC_CarControllerV3.SteeringWheelRotateAround.ZAxis => Vector3.forward,
            _ => Vector3.forward
        };
    }
    
    #endregion RCC Stuff

    #region Rotation Tracking

    private readonly List<Transform> _trackedTransforms = new();
    private readonly List<Vector3> _lastPositions = new();
    private readonly List<float> _totalAngles = new();
    private bool _isTracking;
    private float _averageAngle;
    
    private void StartTrackingTransform(Transform trans)
    {
        if (trans == null) return;
        
        _trackedTransforms.Add(trans);
        _lastPositions.Add(GetLocalPositionWithoutRotation(transform.position));
        _totalAngles.Add(0f);
        _isTracking = true;
    }

    private void StopTrackingTransform(Transform trans)
    {
        int index = _trackedTransforms.IndexOf(trans);
        if (index != -1)
        {
            _trackedTransforms.RemoveAt(index);
            _lastPositions.RemoveAt(index);
            _totalAngles.RemoveAt(index);
        }
        
        _isTracking = _trackedTransforms.Count > 0;
    }

    private void UpdateRotationTracking()
    {
        if (!_isTracking || _trackedTransforms.Count == 0) return;

        Vector3 trackingAxis = GetSteeringWheelLocalAxis();
        
        for (int i = 0; i < _trackedTransforms.Count; i++)
        {
            if (_trackedTransforms[i] == null) continue;

            Vector3 currentPosition = GetLocalPositionWithoutRotation(_trackedTransforms[i].position);
            if (currentPosition == _lastPositions[i]) continue;

            Vector3 previousVector = _lastPositions[i];
            Vector3 currentVector = currentPosition;

            previousVector = Vector3.ProjectOnPlane(previousVector, trackingAxis).normalized;
            currentVector = Vector3.ProjectOnPlane(currentVector, trackingAxis).normalized;

            if (previousVector.sqrMagnitude > 0.001f && currentVector.sqrMagnitude > 0.001f)
            {
                float deltaAngle = Vector3.SignedAngle(previousVector, currentVector, trackingAxis);
                if (Mathf.Abs(deltaAngle) < 90f) _totalAngles[i] += deltaAngle; // Prevent big tracking jumps
            }

            _lastPositions[i] = currentPosition;
        }

        // Calculate average every frame using only valid transforms
        float sumAngles = 0f;
        int validTransforms = 0;
        
        for (int i = 0; i < _trackedTransforms.Count; i++)
        {
            if (_trackedTransforms[i] == null) continue;
            sumAngles += _totalAngles[i];
            validTransforms++;
        }

        if (validTransforms > 0)
            _averageAngle = sumAngles / validTransforms;
    }
    
    private float GetNormalizedValue()
    {
        float maxRange = GetMaxSteeringRange();
        // return Mathf.Clamp(_averageAngle / (maxRange * 0.5f), -1f, 1f);
        return Mathf.Clamp(_averageAngle / (maxRange), -1f, 1f) * GetSteeringWheelSign();
    }
    
    private Vector3 GetLocalPositionWithoutRotation(Vector3 worldPosition)
    {
        Transform steeringTransform = _carController.SteeringWheel;
        
        Quaternion localRotation = steeringTransform.localRotation;
        steeringTransform.localRotation = _carController.orgSteeringWheelRot;
        
        Vector3 localPosition = steeringTransform.InverseTransformPoint(worldPosition);
        steeringTransform.localRotation = localRotation;
        
        return localPosition;
    }

    #endregion Rotation Tracking

    public override void OnGrab(InteractionContext context, Vector3 grabPoint)
    {
        if (ControllerRay?.pivotPoint == null)
            return;
        
        StartTrackingTransform(ControllerRay.transform);
    }

    public override void OnDrop(InteractionContext context)
    {
        if (ControllerRay?.transform != null)
            StopTrackingTransform(ControllerRay.transform);
    }

    private void Update()
    {
        if (!IsPickedUp || ControllerRay?.pivotPoint == null || _carController == null)
            return;

        UpdateRotationTracking();
    }

    #region Unused Abstract Method Implementations

    public override void OnUseDown(InteractionContext context) { }
    public override void OnUseUp(InteractionContext context) { }
    public override void OnFlingTowardsTarget(Vector3 target) { }

    #endregion Unused Abstract Method Implementations
}