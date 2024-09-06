using System.Collections;
using ABI_RC.Core;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.InteractionTest.Components;

public class InteractionTracker : MonoBehaviour
{
    #region Setup

    public static void Setup(GameObject parentObject, bool isLeft = true)
    {
        // LeapMotion: RotationTarget
        
        GameObject trackerObject = new("NAK.InteractionTracker");
        trackerObject.transform.SetParent(parentObject.transform);
        trackerObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        GameObject ikObject = new("NAK.InteractionTracker.IK");
        ikObject.transform.SetParent(trackerObject.transform);
        ikObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        SphereCollider sphereCol = trackerObject.AddComponent<SphereCollider>();
        sphereCol.radius = 0f;
        sphereCol.isTrigger = true;
        
        BetterBetterCharacterController.QueueRemovePlayerCollision(sphereCol);
        
        InteractionTracker tracker = trackerObject.AddComponent<InteractionTracker>();
        tracker.isLeft = isLeft;
        tracker.Initialize();
    }

    #endregion Setup
    
    #region Actions

    public Action OnPenetrationDetected; // called on start of penetration
    public Action OnPenetrationLost; // called on end of penetration
    public Action OnPenetrationNormalChanged; // called when penetration normal changes after 2 degree threshold

    #endregion Actions

    public bool isLeft;
    
    public bool IsColliding => _isColliding;
    public Vector3 ClosestPoint { get; private set; }
    public Vector3 LastPenetrationNormal => _lastPenetrationNormal;
    
    private bool _isColliding;
    private bool _wasPenetrating;
    public Vector3 _lastPenetrationNormal = Vector3.forward;
    private Collider _selfCollider;
    private const float NormalChangeThreshold = 0.2f;

    #region Unity Events
    
    private void Initialize()
    {
        _selfCollider = GetComponent<Collider>();
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoaded);
    }

    private void OnDestroy()
    {
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.RemoveListener(OnLocalAvatarLoaded);
    }
    
    private void OnLocalAvatarLoaded(CVRAvatar _)
    {
        StartCoroutine(FrameLateInit());
    }

    private IEnumerator FrameLateInit()
    {
        yield return null;
        yield return null;
        OnInitSolver();
        IKSystem.vrik.onPreSolverUpdate.AddListener(OnPreSolverUpdate);
        IKSystem.vrik.onPostSolverUpdate.AddListener(OnPostSolverUpdate);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == CVRLayers.PlayerLocal)
            return;
        
        if (_selfCollider == null)
            return;

        Transform selfTransform = transform;
        Transform otherTransform = other.transform;

        bool isPenetrating = Physics.ComputePenetration(
            _selfCollider, selfTransform.position, selfTransform.rotation,
            other, otherTransform.position, otherTransform.rotation,
            out Vector3 direction, out float distance);

        if (isPenetrating)
        {
            ClosestPoint = selfTransform.position + direction * distance;
            Debug.DrawRay(ClosestPoint, direction * 10, Color.red);

            if (!_wasPenetrating)
            {
                OnPenetrationDetected?.Invoke();
                _wasPenetrating = true;
                _lastPenetrationNormal = direction;
            }

            float angleChange = Vector3.Angle(_lastPenetrationNormal, direction);
            Debug.Log("Angle change: " + angleChange);
            if (angleChange > NormalChangeThreshold)
            {
                _lastPenetrationNormal = direction;
                OnPenetrationNormalChanged?.Invoke();
            }
        }
        else
        {
            if (_wasPenetrating)
            {
                OnPenetrationLost?.Invoke();
                _wasPenetrating = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == CVRLayers.PlayerLocal)
            return;
        
        Debug.Log("Triggered with " + other.gameObject.name);
        _isColliding = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == CVRLayers.PlayerLocal)
            return;
        
        Debug.Log("Exited trigger with " + other.gameObject.name);
        _isColliding = false;

        if (_wasPenetrating)
        {
            OnPenetrationLost?.Invoke();
            _wasPenetrating = false;
        }
    }

    #endregion Unity Events

    private Transform _oldTarget;
    
    private Vector3 _initialPosOffset;
    private Quaternion _initialRotOffset;

    private IKSolverVR.Arm _armSolver;
    
    private void OnInitSolver()
    {
        _armSolver = isLeft ? IKSystem.vrik.solver.arms[0] : IKSystem.vrik.solver.arms[1];

        Transform target = _armSolver.target;
        if (target == null)
            target = transform.parent.Find("RotationTarget"); // LeapMotion: RotationTarget
        
        if (target == null) return;
        
        _initialPosOffset = target.localPosition;
        _initialRotOffset = target.localRotation;
    }
    
    private void OnPreSolverUpdate()
    {
        if (!IsColliding) 
            return;
        
        Transform selfTransform = transform;
        
        float dot = Vector3.Dot(_lastPenetrationNormal, selfTransform.forward);
        if (dot > -0.45f) 
            return;

        _oldTarget = _armSolver.target;
        _armSolver.target = selfTransform.GetChild(0);

        _armSolver.target.position = ClosestPoint + selfTransform.rotation * _initialPosOffset;
        _armSolver.target.rotation = _initialRotOffset * Quaternion.LookRotation(-_lastPenetrationNormal, selfTransform.up);
        
        _armSolver.positionWeight = 1f;
        _armSolver.rotationWeight = 1f;
    }

    private void OnPostSolverUpdate()
    {
        if (!_oldTarget) 
            return;

        _armSolver.target = _oldTarget;
        _oldTarget = null;
        
        _armSolver.positionWeight = 0f;
        _armSolver.rotationWeight = 0f;
    }
}