using ABI_RC.Core.Player;
using ABI_RC.Systems.InputManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

/*
 * Physics Gun script from repository - https://github.com/Laumania/Unity3d-PhysicsGun
 * Created by:
 * Mads Laumann, https://github.com/laumania
 * WarmedxMints, https://github.com/WarmedxMints
 *
 * Original/initial script "Gravity Gun": https://pastebin.com/w1G8m3dH
 * Original author: Jake Perry, reddit.com/user/nandos13
*/

namespace NAK.PhysicsGunMod.Components;

public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    public static PhysicsGunInteractionBehavior Instance;
    
    [Header("LayerMask")] [Tooltip("The layer which the gun can grab objects from")] [SerializeField]
    private LayerMask _grabLayer;

    [SerializeField] private Camera _camera;

    [Header("Input Setting")] [Space(10)] public KeyCode Rotate = KeyCode.R;
    public KeyCode SnapRotation = KeyCode.LeftShift;
    public KeyCode SwitchAxis = KeyCode.Tab;
    public KeyCode RotateZ = KeyCode.Space;
    public KeyCode RotationSpeedIncrease = KeyCode.LeftControl;
    public KeyCode ResetRotation = KeyCode.LeftAlt;

    /// <summary>The rigidbody we are currently holding</summary>
    private Rigidbody _grabbedRigidbody;

    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3 _hitOffsetLocal;

    /// <summary>The distance we are holding the object at</summary>
    private float _currentGrabDistance;

    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation _initialInterpolationSetting;

    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Quaternion _rotationDifference;

    /// <summary>The start point for the Laser. This will typically be on the end of the gun</summary>
    [SerializeField] private Transform _laserStartPoint;

    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector3 _rotationInput = Vector3.zero;

    [Header("Rotation Settings")] [Tooltip("Transform of the player, that rotations should be relative to")]
    public Transform PlayerTransform;

    [SerializeField] private float _rotationSenstivity = 10f;

    public float SnapRotationDegrees = 45f;
    [SerializeField] private float _snappedRotationSens = 25f;
    [SerializeField] private float _rotationSpeed = 10f;

    private Quaternion _desiredRotation = Quaternion.identity;

    [SerializeField] [Tooltip("Input values above this will be considered and intentional change in rotation")]
    private float _rotationTollerance = 0.8f;

    private bool m_UserRotation;

    public bool UserRotation
    {
        get => m_UserRotation;
        private set
        {
            if (m_UserRotation == value) 
                return;
            
            m_UserRotation = value;
            OnRotation?.Invoke(value);
        }
    }

    private bool m_SnapRotation;

    private bool _snapRotation
    {
        get => m_SnapRotation;
        set
        {
            if (m_SnapRotation == value)
                return;
            
            m_SnapRotation = value;
            OnRotationSnapped?.Invoke(value);
        }
    }

    private bool m_RotationAxis;

    private bool _rotationAxis
    {
        get => m_RotationAxis;
        set
        {
            if (m_RotationAxis == value) 
                return;
            
            m_RotationAxis = value;
            OnAxisChanged?.Invoke(value);
        }
    }

    private Vector3 _lockedRot;

    private Vector3 _forward;
    private Vector3 _up;
    private Vector3 _right;
    
    //ScrollWheel ObjectMovement
    private Vector3 _scrollWheelInput = Vector3.zero;

    [Header("Scroll Wheel Object Movement")] [Space(5)] [SerializeField]
    private float _scrollWheelSensitivity = 5f;

    [SerializeField] [Tooltip("The min distance the object can be from the player")]
    private float _minObjectDistance = 2.5f;

    /// <summary>The maximum distance at which a new object can be picked up</summary>
    [SerializeField] [Tooltip("The maximum distance at which a new object can be picked up")]
    private float _maxGrabDistance = 50f;

    private bool _distanceChanged;

    //Vector3.Zero and Vector2.zero create a new Vector3 each time they are called so these simply save that process and a small amount of cpu runtime.
    private readonly Vector3 _zeroVector3 = Vector3.zero;
    private readonly Vector3 _oneVector3 = Vector3.one;
    private readonly Vector3 _zeroVector2 = Vector2.zero;

    private bool _justReleased;
    private bool _wasKinematic;

    [Serializable]
    public class BoolEvent : UnityEvent<bool> {}

    [Serializable]
    public class GrabEvent : UnityEvent<GameObject> {}

    [Header("Events")] [Space(10)] 
    public BoolEvent OnRotation;
    public BoolEvent OnRotationSnapped;
    public BoolEvent OnAxisChanged;

    public GrabEvent OnObjectGrabbed;
    public Func<GameObject, bool> OnObjectReleased;
    
    // pre event for OnPreGrabbedObject, return false to cancel grab
    public Func<GameObject, bool> OnPreGrabbedObject;
    // public Action<GameObject> OnGrabbedObject;

    //public properties for the Axis Arrows.  These are optional and can be safely removed
    // public Vector3 CurrentForward => _forward;
    // public Vector3 CurrentUp => _up;
    // public Vector3 CurrentRight => _right;

    /// <summary>The transfor of the rigidbody we are holding</summary>
    public Transform CurrentGrabbedTransform { get; private set; }

    //public properties for the Line Renderer
    public Vector3 StartPoint { get; private set; }
    public Vector3 MidPoint { get; private set; }
    public Vector3 EndPoint { get; private set; }

    private void Start()
    {
        if (Instance != null
            && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        gameObject.AddComponent<ObjectSyncBridge>();
        
        _camera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        PlayerTransform = PlayerSetup.Instance.transform; // TODO: this might be fucked in VR
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!Input.GetMouseButton(0))
        {
            // We are not holding the mouse button. Release the object and return before checking for a new one
            if (_grabbedRigidbody != null) ReleaseObject();

            _justReleased = false;
            return;
        }

        if (_grabbedRigidbody == null && !_justReleased)
        {
            // We are not holding an object, look for one to pick up
            Ray ray = CenterRay();
            RaycastHit hit;

            //Just so These aren't included in a build
#if UNITY_EDITOR
            Debug.DrawRay(ray.origin, ray.direction * _maxGrabDistance, Color.blue, 0.01f);
#endif
            if (Physics.Raycast(ray, out hit, _maxGrabDistance, _grabLayer))
                // Don't pick up kinematic rigidbodies (they can't move)
                if (hit.rigidbody != null /*&& !hit.rigidbody.isKinematic*/)
                {
                    // Check if we are allowed to pick up this object
                    if (OnPreGrabbedObject != null 
                        && !OnPreGrabbedObject(hit.rigidbody.gameObject))
                        return;
                    
                    // Track rigidbody's initial information
                    _grabbedRigidbody = hit.rigidbody;
                    _wasKinematic = _grabbedRigidbody.isKinematic;
                    _grabbedRigidbody.isKinematic = false;
                    _grabbedRigidbody.freezeRotation = true;
                    _initialInterpolationSetting = _grabbedRigidbody.interpolation;
                    _rotationDifference = Quaternion.Inverse(PlayerTransform.rotation) * _grabbedRigidbody.rotation;
                    _hitOffsetLocal = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
                    _currentGrabDistance = hit.distance; // Vector3.Distance(ray.origin, hit.point);
                    CurrentGrabbedTransform = _grabbedRigidbody.transform;
                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    _grabbedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                    OnObjectGrabbed?.Invoke(_grabbedRigidbody.gameObject);

#if UNITY_EDITOR
                    Debug.DrawRay(hit.point, hit.normal * 10f, Color.red, 10f);
#endif
                }
        }
        else if (_grabbedRigidbody != null)
        {
            UserRotation = Input.GetKey(Rotate);

            if (Input.GetKeyDown(Rotate)) 
                _desiredRotation = _grabbedRigidbody.rotation;

            if (Input.GetKey(ResetRotation))
            {
                _desiredRotation = Quaternion.identity;
            }

            // We are already holding an object, listen for rotation input
            if (Input.GetKey(Rotate))
            {
                var rotateZ = Input.GetKey(RotateZ);

                var increaseSens = Input.GetKey(RotationSpeedIncrease) ? 2.5f : 1f;

                if (Input.GetKeyDown(SwitchAxis))
                {
                    _rotationAxis = !_rotationAxis;

                    OnAxisChanged?.Invoke(_rotationAxis);
                }

                //Snap Object nearest _snapRotationDegrees
                if (Input.GetKeyDown(SnapRotation))
                {
                    _snapRotation = true;

                    Vector3 newRot = _grabbedRigidbody.transform.rotation.eulerAngles;

                    newRot.x = Mathf.Round(newRot.x / SnapRotationDegrees) * SnapRotationDegrees;
                    newRot.y = Mathf.Round(newRot.y / SnapRotationDegrees) * SnapRotationDegrees;
                    newRot.z = Mathf.Round(newRot.z / SnapRotationDegrees) * SnapRotationDegrees;

                    Quaternion rot = Quaternion.Euler(newRot);

                    _desiredRotation = rot;
                    //_grabbedRigidbody.MoveRotation(rot);
                }
                else if (Input.GetKeyUp(SnapRotation))
                {
                    _snapRotation = false;
                }

                var x = Input.GetAxisRaw("Mouse X");
                var y = Input.GetAxisRaw("Mouse Y");

                if (Mathf.Abs(x) > _rotationTollerance)
                {
                    _rotationInput.x = rotateZ ? 0f : x * _rotationSenstivity * increaseSens;
                    _rotationInput.z = rotateZ ? x * _rotationSenstivity * increaseSens : 0f;
                }

                if (Mathf.Abs(y) > _rotationTollerance) _rotationInput.y = y * _rotationSenstivity * increaseSens;
            }
            else
            {
                _snapRotation = false;
            }

            var direction = Input.GetAxis("Mouse ScrollWheel");

            //Optional Keyboard inputs
            if (Input.GetKeyDown(KeyCode.T))
                direction = -0.1f;
            else if (Input.GetKeyDown(KeyCode.G))
                direction = 0.1f;

            if (Mathf.Abs(direction) > 0 && CheckObjectDistance(direction))
            {
                _distanceChanged = true;
                _scrollWheelInput = PlayerTransform.forward * (_scrollWheelSensitivity * direction);
            }
            else
            {
                _scrollWheelInput = _zeroVector3;
            }

            if (Input.GetMouseButtonDown(1))
            {
                //To prevent warnings in the inpector
                _grabbedRigidbody.collisionDetectionMode = !_wasKinematic
                    ? CollisionDetectionMode.ContinuousSpeculative
                    : CollisionDetectionMode.Continuous;
                _grabbedRigidbody.isKinematic = _wasKinematic = !_wasKinematic;

                _justReleased = true;
                ReleaseObject();
            }
        }
    }

    private void FixedUpdate()
    {
        if (_grabbedRigidbody)
        {
            // We are holding an object, time to rotate & move it
            Ray ray = CenterRay();

            UpdateRotationAxis();

#if UNITY_EDITOR
            Debug.DrawRay(_grabbedTransform.position, _up * 5f      , Color.green);
            Debug.DrawRay(_grabbedTransform.position, _right * 5f   , Color.red);
            Debug.DrawRay(_grabbedTransform.position, _forward * 5f , Color.blue);
#endif
            // Apply any intentional rotation input made by the player & clear tracked input
            Quaternion intentionalRotation = Quaternion.AngleAxis(_rotationInput.z, _forward) *
                                             Quaternion.AngleAxis(_rotationInput.y, _right) *
                                             Quaternion.AngleAxis(-_rotationInput.x, _up) * _desiredRotation;
            Quaternion relativeToPlayerRotation = PlayerTransform.rotation * _rotationDifference;

            if (UserRotation && _snapRotation)
            {
                //Add mouse movement to vector so we can measure the amount of movement
                _lockedRot += _rotationInput;

                //If the mouse has moved far enough to rotate the snapped object
                if (Mathf.Abs(_lockedRot.x) > _snappedRotationSens || Mathf.Abs(_lockedRot.y) > _snappedRotationSens ||
                    Mathf.Abs(_lockedRot.z) > _snappedRotationSens)
                {
                    for (var i = 0; i < 3; i++)
                        if (_lockedRot[i] > _snappedRotationSens)
                            _lockedRot[i] += SnapRotationDegrees;
                        else if (_lockedRot[i] < -_snappedRotationSens)
                            _lockedRot[i] += -SnapRotationDegrees;
                        else
                            _lockedRot[i] = 0;

                    Quaternion q = Quaternion.AngleAxis(-_lockedRot.x, _up) *
                                   Quaternion.AngleAxis(_lockedRot.y, _right) *
                                   Quaternion.AngleAxis(_lockedRot.z, _forward) * _desiredRotation;

                    Vector3 newRot = q.eulerAngles;

                    newRot.x = Mathf.Round(newRot.x / SnapRotationDegrees) * SnapRotationDegrees;
                    newRot.y = Mathf.Round(newRot.y / SnapRotationDegrees) * SnapRotationDegrees;
                    newRot.z = Mathf.Round(newRot.z / SnapRotationDegrees) * SnapRotationDegrees;

                    _desiredRotation = Quaternion.Euler(newRot);

                    _lockedRot = _zeroVector2;
                }
            }
            else
            {
                //Rotate the object to remain consistent with any changes in player's rotation
                _desiredRotation = UserRotation ? intentionalRotation : relativeToPlayerRotation;
            }

            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            _grabbedRigidbody.angularVelocity = _zeroVector3;
            _rotationInput = _zeroVector2;
            _rotationDifference = Quaternion.Inverse(PlayerTransform.rotation) * _desiredRotation;

            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            Vector3 holdPoint = ray.GetPoint(_currentGrabDistance) + _scrollWheelInput;
            Vector3 centerDestination = holdPoint - CurrentGrabbedTransform.TransformVector(_hitOffsetLocal);

#if UNITY_EDITOR
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);
#endif
            // Find vector from current position to destination
            Vector3 toDestination = centerDestination - CurrentGrabbedTransform.position;

            // Calculate force
            Vector3 force = toDestination / Time.fixedDeltaTime * 0.3f/* / _grabbedRigidbody.mass*/;

            //force += _scrollWheelInput;
            // Remove any existing velocity and add force to move to final position
            _grabbedRigidbody.velocity = _zeroVector3;
            _grabbedRigidbody.AddForce(force, ForceMode.VelocityChange);

            //Rotate object
            RotateGrabbedObject();

            //We need to recalculte the grabbed distance as the object distance from the player has been changed
            if (_distanceChanged)
            {
                _distanceChanged = false;
                _currentGrabDistance = Vector3.Distance(ray.origin, holdPoint);
            }

            //Update public properties
            StartPoint = _laserStartPoint.transform.position;
            MidPoint = holdPoint;
            EndPoint = CurrentGrabbedTransform.TransformPoint(_hitOffsetLocal);
        }
    }

    private void RotateGrabbedObject()
    {
        if (_grabbedRigidbody == null)
            return;
        
        // lerp to desired rotation
        _grabbedRigidbody.MoveRotation(Quaternion.Lerp(_grabbedRigidbody.rotation, _desiredRotation,
            Time.fixedDeltaTime * _rotationSpeed));
    }

    //Update Rotation axis based on movement
    private void UpdateRotationAxis()
    {
        if (!_snapRotation)
        {
            _forward = PlayerTransform.forward;
            _right = PlayerTransform.right;
            _up = PlayerTransform.up;

            return;
        }

        if (_rotationAxis)
        {
            _forward = CurrentGrabbedTransform.forward;
            _right = CurrentGrabbedTransform.right;
            _up = CurrentGrabbedTransform.up;

            return;
        }

        NearestTranformDirection(CurrentGrabbedTransform, PlayerTransform, ref _up, ref _forward, ref _right);
    }

    private void NearestTranformDirection(Transform transformToCheck, Transform referenceTransform, ref Vector3 up,
        ref Vector3 forward, ref Vector3 right)
    {
        var directions = new List<Vector3>
        {
            transformToCheck.forward,
            -transformToCheck.forward,
            transformToCheck.up,
            -transformToCheck.up,
            transformToCheck.right,
            -transformToCheck.right
        };

        //Find the up Vector
        up = GetDirectionVector(directions, referenceTransform.up);
        //Remove Vectors from list to prevent duplicates and the opposite vector being found in case where the player is at around a 45 degree angle to the object
        directions.Remove(up);
        directions.Remove(-up);
        //Find the Forward Vector       
        forward = GetDirectionVector(directions, referenceTransform.forward);
        //Remove used directions
        directions.Remove(forward);
        directions.Remove(-forward);

        right = GetDirectionVector(directions, referenceTransform.right);
    }

    private Vector3 GetDirectionVector(List<Vector3> directions, Vector3 direction)
    {
        var maxDot = -Mathf.Infinity;
        Vector3 ret = Vector3.zero;

        for (var i = 0; i < directions.Count; i++)
        {
            var dot = Vector3.Dot(direction, directions[i]);

            if (dot > maxDot)
            {
                ret = directions[i];
                maxDot = dot;
            }
        }

        return ret;
    }

    /// <returns>Ray from center of the main camera's viewport forward</returns>
    private Ray CenterRay()
    {
        return _camera.ViewportPointToRay(_oneVector3 * 0.5f);
    }

    //Check distance is within range when moving object with the scroll wheel
    private bool CheckObjectDistance(float direction)
    {
        Vector3 pointA = PlayerTransform.position;
        Vector3 pointB = _grabbedRigidbody.position;

        var distance = Vector3.Distance(pointA, pointB);

        if (direction > 0)
            return distance <= _maxGrabDistance;

        if (direction < 0)
            return distance >= _minObjectDistance;

        return false;
    }

    private void ReleaseObject()
    {
        if (OnObjectReleased != null)
            OnObjectReleased(_grabbedRigidbody.gameObject);
        
        if (_grabbedRigidbody)
        {
            //Move rotation to desired rotation in case the lerp hasn't finished
            //_grabbedRigidbody.MoveRotation(_desiredRotation);
            // Reset the rigidbody to how it was before we grabbed it
            _grabbedRigidbody.isKinematic = _wasKinematic;
            _grabbedRigidbody.interpolation = _initialInterpolationSetting;
            _grabbedRigidbody.freezeRotation = false;
            _grabbedRigidbody = null;
        }
        
        _scrollWheelInput = _zeroVector3;
        CurrentGrabbedTransform = null;
        UserRotation = false;
        _snapRotation = false;
        StartPoint = _zeroVector3;
        MidPoint = _zeroVector3;
        EndPoint = _zeroVector3;

        OnObjectGrabbed?.Invoke(null);
    }
}