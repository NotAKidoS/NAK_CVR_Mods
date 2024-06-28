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
        trackerObject.AddComponent<InteractionTracker>().isLeft = isLeft;
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
    
    private void Awake()
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
    
    private void OnPreSolverUpdate()
    {
        if (!IsColliding) return;
        
        var solverArms = IKSystem.vrik.solver.arms;
        IKSolverVR.Arm arm = isLeft ? solverArms[0] : solverArms[1];

        _oldTarget = arm.target;
        arm.target = transform.GetChild(0);
        
        arm.target.position = ClosestPoint;
        arm.target.rotation = Quaternion.LookRotation(_lastPenetrationNormal, _oldTarget.rotation * Vector3.up);
        
        arm.positionWeight = 1f;
        arm.rotationWeight = 1f;
    }

    private void OnPostSolverUpdate()
    {
        if (!_oldTarget) 
            return;
        
        var solverArms = IKSystem.vrik.solver.arms;
        IKSolverVR.Arm arm = isLeft ? solverArms[0] : solverArms[1];
        arm.target = _oldTarget;
        _oldTarget = null;
        
        arm.positionWeight = 0f;
        arm.rotationWeight = 0f;
    }
}