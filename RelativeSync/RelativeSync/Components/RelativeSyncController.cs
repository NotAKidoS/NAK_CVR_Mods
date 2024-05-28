using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.RelativeSync.Components;

[DefaultExecutionOrder(int.MaxValue)] // make sure this runs after NetIKController
public class RelativeSyncController : MonoBehaviour
{
    private const float MaxMagnitude = 750000000000f;

    private float _updateInterval = 0.05f;
    private float _lastUpdate;

    private string _userId;
    private PuppetMaster puppetMaster { get; set; }
    private RelativeSyncMarker _relativeSyncMarker;

    private RelativeSyncData _relativeSyncData;
    private RelativeSyncData _lastSyncData;

    #region Unity Events

    private void Start()
    {
        puppetMaster = GetComponent<PuppetMaster>();

        _userId = puppetMaster._playerDescriptor.ownerId;
        RelativeSyncManager.RelativeSyncControllers.Add(_userId, this);
    }

    private void OnDestroy()
    {
        RelativeSyncManager.RelativeSyncControllers.Remove(_userId);
    }

    private void LateUpdate()
    {
        if (puppetMaster._isHidden)
            return;

        if (_relativeSyncMarker == null)
            return;
        
        Animator animator = puppetMaster._animator;
        if (animator == null)
            return;

        Transform avatarTransform = animator.transform;
        Transform hipTrans = (animator.avatar != null && animator.isHuman)
            ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
        
        // TODO: handle the case where hip is not synced but is found on remote client

        float lerp = Mathf.Min((Time.time - _lastUpdate) / _updateInterval, 1f);
        
        ApplyRelativeRotation(avatarTransform, hipTrans, lerp);
        ApplyRelativePosition(hipTrans, lerp);
        
        avatarTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); // idk if needed
    }

    private void ApplyRelativeRotation(Transform avatarTransform, Transform hipTransform, float lerp)
    {
        if (!_relativeSyncMarker.ApplyRelativeRotation ||
            !(_relativeSyncData.LocalRootRotation.sqrMagnitude < MaxMagnitude)) 
            return; // not applying relative rotation or data is invalid

        Quaternion markerRotation = _relativeSyncMarker.transform.rotation;
        Quaternion lastWorldRotation = markerRotation * Quaternion.Euler(_lastSyncData.LocalRootRotation);
        Quaternion worldRotation = markerRotation * Quaternion.Euler(_relativeSyncData.LocalRootRotation);
        
        Quaternion lastWorldHipRotation = markerRotation * Quaternion.Euler(_lastSyncData.LocalHipRotation);
        Quaternion worldHipRotation = markerRotation * Quaternion.Euler(_relativeSyncData.LocalHipRotation);

        if (_relativeSyncMarker.OnlyApplyRelativeHeading)
        {
            Vector3 currentWorldUp = avatarTransform.up;
            
            Vector3 currentForward = lastWorldRotation * Vector3.forward;
            Vector3 targetForward = worldRotation * Vector3.forward;
            currentForward = Vector3.ProjectOnPlane(currentForward, currentWorldUp).normalized;
            targetForward = Vector3.ProjectOnPlane(targetForward, currentWorldUp).normalized;
            lastWorldRotation = Quaternion.LookRotation(currentForward, currentWorldUp);
            worldRotation = Quaternion.LookRotation(targetForward, currentWorldUp);
            
            if (hipTransform != null)
            {
                currentForward = lastWorldHipRotation * Vector3.forward;
                targetForward = worldHipRotation * Vector3.forward;
                currentForward = Vector3.ProjectOnPlane(currentForward, currentWorldUp).normalized;
                targetForward = Vector3.ProjectOnPlane(targetForward, currentWorldUp).normalized;
                lastWorldHipRotation = Quaternion.LookRotation(currentForward, currentWorldUp);
                worldHipRotation = Quaternion.LookRotation(targetForward, currentWorldUp);
            }
        }

        transform.rotation = Quaternion.Slerp(lastWorldRotation, worldRotation, lerp);
        if (hipTransform != null) hipTransform.rotation = Quaternion.Slerp(lastWorldHipRotation, worldHipRotation, lerp);
    }

    private void ApplyRelativePosition(Transform hipTransform, float lerp)
    {
        if (!_relativeSyncMarker.ApplyRelativePosition ||
            !(_relativeSyncData.LocalRootPosition.sqrMagnitude < MaxMagnitude)) 
            return; // not applying relative position or data is invalid

        Transform targetTransform = _relativeSyncMarker.transform;
        
        Vector3 lastWorldPosition = targetTransform.TransformPoint(_lastSyncData.LocalRootPosition);
        Vector3 worldPosition = targetTransform.TransformPoint(_relativeSyncData.LocalRootPosition);
        transform.position = Vector3.Lerp(lastWorldPosition, worldPosition, lerp);

        if (hipTransform == null) 
            return;
        
        Vector3 lastWorldHipPosition = targetTransform.TransformPoint(_lastSyncData.LocalHipPosition);
        Vector3 worldHipPosition = targetTransform.TransformPoint(_relativeSyncData.LocalHipPosition);
        hipTransform.position = Vector3.Lerp(lastWorldHipPosition, worldHipPosition, lerp);
    }

    #endregion Unity Events

    #region Public Methods

    public void SetRelativeSyncMarker(RelativeSyncMarker target)
    {
        if (_relativeSyncMarker == target)
            return;

        _relativeSyncMarker = target;

        // calculate relative position and rotation so lerp can smooth it out (hack)
        if (_relativeSyncMarker == null) 
            return;
        
        Animator avatarAnimator = puppetMaster._animator;
        if (avatarAnimator == null)
            return; // i dont care to bother
        
        RelativeSyncManager.GetRelativeAvatarPositionsFromMarker(
            avatarAnimator, _relativeSyncMarker.transform,
            out Vector3 relativePosition, out Vector3 relativeRotation,
            out Vector3 relativeHipPosition, out Vector3 relativeHipRotation);

        // set last sync data to current position and rotation so we don't lerp from the last marker
        _lastSyncData.LocalRootPosition = relativePosition;
        _lastSyncData.LocalRootRotation = relativeRotation;
        _lastSyncData.LocalHipPosition = relativeHipPosition;
        _lastSyncData.LocalHipRotation = relativeHipRotation;
    }

    public void SetRelativePositions(
        Vector3 position, Vector3 rotation,
        Vector3 hipPosition, Vector3 hipRotation)
    {
        // calculate update interval
        float prevUpdate = _lastUpdate;
        _lastUpdate = Time.time;
        _updateInterval = _lastUpdate - prevUpdate;
        
        // cycle last sync data
        _lastSyncData = _relativeSyncData;

        // set new sync data
        _relativeSyncData.LocalRootPosition = position;
        _relativeSyncData.LocalRootRotation = rotation;
        _relativeSyncData.LocalHipPosition = hipPosition;
        _relativeSyncData.LocalHipRotation = hipRotation;
    }

    #endregion Public Methods

    private struct RelativeSyncData
    {
        public Vector3 LocalRootPosition;
        public Vector3 LocalRootRotation;
        public Vector3 LocalHipPosition;
        public Vector3 LocalHipRotation;
    }
}