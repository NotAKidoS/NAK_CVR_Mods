using ABI_RC.Core.InteractionSystem.Base;
using ABI.CCK.Components;
using UnityEngine;

// https://wirewhiz.com/vr-grabbing-tutorial/

public class CollisionLogger : MonoBehaviour
{
    private int _touchCount;
    public int touchCount
    {
        get => _touchCount;
        private set
        {
            if (!countChangedThisFrame && _touchCount != value) countChangedThisFrame = true;
            _touchCount = Mathf.Max(0, value);
        }
    }
    
    public bool countChangedThisFrame {get; private set;} = true;
    
    private void OnCollisionEnter(Collision _)
        => touchCount++;
    private void OnCollisionExit(Collision _)
        => touchCount--;
    private void LateUpdate()
        => countChangedThisFrame = false;
}

public class FixedJointPickupable : Pickupable
{
    private Rigidbody _rigidbody;
    private bool _originalGravityState;
    
    private Vector3 _initialGrabOffset;
    private Vector3 _unCollidedOffset;
    private float _unCollideInterpolationTime;
    
    private readonly JointDrive positionDrive = new() { positionSpring = 2000, positionDamper = 10, maximumForce = 3.402823e+38f };
    private readonly JointDrive rotationDrive = new() { positionSpring = 2000, positionDamper = 0, maximumForce = 3.402823e+38f };
    private ConfigurableJoint _configurableJoint;
    private FixedJoint _fixedJoint;
    
    private CollisionLogger _collisionLogger;
    
    private Transform _pickupHandTransform;
    private Vector3 _previousHandPosition;
    private Vector3 _previousHandRotation;
    private float _grabStartTime;

    public override bool CanPickup => IsPickupable && isActiveAndEnabled;

    public override bool DisallowTheft => false;

    public override bool IsAutoHold => false;

    public override float MaxGrabDistance => 100f;

    public override float MaxPushDistance => 100f;

    public override bool IsObjectRotationAllowed => true;

    public override bool IsObjectPushPullAllowed => true;

    public override bool IsObjectInteractionAllowed => true;

    public override Transform RootTransform => base.transform;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
        }
    }

    public override void OnGrab(Vector3 grabPoint)
    {
        _grabStartTime = Time.time;
        _pickupHandTransform = ControllerRay.pivotPoint.transform;
        
        ControllerRay.UpdateGrabDistance(Vector3.Distance(ControllerRay.transform.position, grabPoint));
        _pickupHandTransform.rotation = transform.rotation;
        
        // handTransform in local space of the object (initial rotation is already known)
        _initialGrabOffset = transform.InverseTransformPoint(grabPoint);
        
        // ensure our object has a CollisionLogger (hack)
        if (!gameObject.TryGetComponent(out _collisionLogger))
            _collisionLogger = gameObject.AddComponent<CollisionLogger>();
        
        // ensure ControllerRay has a rigidbody (hack)
        if (!_pickupHandTransform.TryGetComponent(out Rigidbody handRigidBody))
        {
            handRigidBody = _pickupHandTransform.gameObject.AddComponent<Rigidbody>();
            handRigidBody.isKinematic = true;
            handRigidBody.useGravity = false;
        }
        
        // ensure Pickupable has a fixed joint (hack)
        if (!_pickupHandTransform.gameObject.TryGetComponent(out _fixedJoint))
        {
            _fixedJoint = _pickupHandTransform.gameObject.AddComponent<FixedJoint>();
            _fixedJoint.enableCollision = false;
            _fixedJoint.autoConfigureConnectedAnchor = false;
            _fixedJoint.connectedAnchor = _initialGrabOffset; // it'll snap to this position after interpolation
        }
        
        // ensure Pickupable has a configurable joint (hack)
        if (!_pickupHandTransform.gameObject.TryGetComponent(out _configurableJoint))
        {
            _configurableJoint = _pickupHandTransform.gameObject.AddComponent<ConfigurableJoint>();
            
            _configurableJoint.enableCollision = false;
            _configurableJoint.autoConfigureConnectedAnchor = false;
            
            _configurableJoint.xMotion = ConfigurableJointMotion.Free;
            _configurableJoint.yMotion = ConfigurableJointMotion.Free;
            _configurableJoint.zMotion = ConfigurableJointMotion.Free;
            _configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
            _configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
            _configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
            
            _configurableJoint.xDrive = positionDrive;
            _configurableJoint.yDrive = positionDrive;
            _configurableJoint.zDrive = positionDrive;
            
            _configurableJoint.rotationDriveMode = RotationDriveMode.Slerp;
            _configurableJoint.slerpDrive = rotationDrive;
        }
        
        //_fixedJoint.connectedBody = _rigidbody;
        _configurableJoint.connectedBody = _rigidbody; // start off floppy
        
        _originalGravityState = _rigidbody.useGravity;
        _rigidbody.useGravity = false;
        
        // Reset velocities to avoid unintended motion
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public override void OnDrop()
    {
        if (_collisionLogger != null)
        {
            Destroy(_collisionLogger);
            _collisionLogger = null;
        }
        
        if (_fixedJoint != null)
        {
            Destroy(_fixedJoint);
            _fixedJoint = null;
        }
        
        if (_configurableJoint != null)
        {
            Destroy(_configurableJoint);
            _configurableJoint = null;
        }
        
        _rigidbody.useGravity = _originalGravityState;
    }

    public override void OnFlingTowardsTarget(Vector3 target)
    {
        // Implement fling logic if needed
    }

    private void Update()
    {
        if (!IsPickedUp) 
            return;
        
        if (_unCollideInterpolationTime < 1f)
        {
            _unCollideInterpolationTime += Time.deltaTime / 0.1f; // 0.01s to interpolate to hand
            _unCollideInterpolationTime = Mathf.Min(_unCollideInterpolationTime, 1f);
            
            _fixedJoint.connectedBody = null; // hack so we don't need to lerp connectedAnchor ourselves
            transform.rotation = Quaternion.Slerp(transform.rotation, _pickupHandTransform.rotation, _unCollideInterpolationTime);
            //transform.position = Vector3.Lerp(transform.position, transform.TransformPoint(_initialGrabOffset), _unCollideInterpolationTime); // no work
            _fixedJoint.connectedBody = _rigidbody; // keep interpolation start point
            _fixedJoint.connectedAnchor = Vector3.Lerp(_unCollidedOffset, _initialGrabOffset, _unCollideInterpolationTime);
        }

        if (_collisionLogger.countChangedThisFrame)
        {
            if (_collisionLogger.touchCount == 0)
            {
                _configurableJoint.connectedBody = null;
                _fixedJoint.connectedBody = _rigidbody; // keep interpolation start point
                _fixedJoint.connectedAnchor = _unCollidedOffset = transform.InverseTransformPoint(_pickupHandTransform.position);
                _unCollideInterpolationTime = 0f; // interpolate to hand
            }
            else if(_collisionLogger.touchCount > 0)
            {
                _fixedJoint.connectedBody = null;
                _configurableJoint.connectedBody = _rigidbody;
                _unCollideInterpolationTime = 1f; // no interpolation
            }
        }
        
        UpdatePositionAndRotation();
        
        _previousHandPosition = _pickupHandTransform.position;
        _previousHandRotation = _pickupHandTransform.rotation.eulerAngles;
    }

    private void UpdatePositionAndRotation()
    {
        if (_rigidbody.isKinematic)
        {
            transform.SetPositionAndRotation(_pickupHandTransform.position, _pickupHandTransform.rotation);
        }
    }
}