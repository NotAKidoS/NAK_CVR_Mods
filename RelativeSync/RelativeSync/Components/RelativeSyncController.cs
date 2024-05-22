using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.RelativeSync.Components;

[DefaultExecutionOrder(int.MaxValue)] // make sure this runs after NetIKController
public class RelativeSyncController : MonoBehaviour
{
    private static float MaxMagnitude = 750000000000f;

    private string _userId;
    private PuppetMaster puppetMaster { get; set; }
    private NetIKController netIkController { get; set; }

    // private bool _syncMarkerChangedSinceLastSync;
    private RelativeSyncMarker _relativeSyncMarker;

    private RelativeSyncData _relativeSyncData;
    private RelativeSyncData _lastSyncData;

    #region Unity Events

    private void Start()
    {
        puppetMaster = GetComponent<PuppetMaster>();
        netIkController = GetComponent<NetIKController>();

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

        Vector3 worldRootPos = avatarTransform.position;
        Quaternion worldRootRot = avatarTransform.rotation;

        Vector3 relativeHipPos = default;
        Quaternion relativeHipRot = default;
        Transform hipTrans = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hipTrans != null)
        {
            Vector3 hipPos = hipTrans.position;
            Quaternion hipRot = hipTrans.rotation;

            relativeHipPos = Quaternion.Inverse(worldRootRot) * (hipPos - worldRootPos);
            relativeHipRot = Quaternion.Inverse(worldRootRot) * hipRot;
        }

        // todo: this is fucked and idk why, is technically slightly differing sync rates,
        // but even reimplementing dynamic tps here didnt fix the jitter
        float lerp = netIkController.GetLerpSpeed();
            
        Vector3 targetLocalPosition = _relativeSyncData.LocalRootPosition;
        Quaternion targetLocalRotation = Quaternion.Euler(_relativeSyncData.LocalRootRotation);
        Transform targetTransform = _relativeSyncMarker.transform;

        if (_relativeSyncMarker.ApplyRelativeRotation && _relativeSyncData.LocalRootRotation.sqrMagnitude < MaxMagnitude)
        {
            Quaternion rotation = targetTransform.rotation;
            Quaternion worldRotation = rotation * targetLocalRotation;
            Quaternion lastRotation = rotation * Quaternion.Euler(_lastSyncData.LocalRootRotation);

            if (_relativeSyncMarker.OnlyApplyRelativeHeading)
            {
                Vector3 currentForward = lastRotation * Vector3.forward;
                Vector3 targetForward = worldRotation * Vector3.forward;
                Vector3 currentWorldUp = avatarTransform.up; // up direction of player before we touch it

                // project forward vectors to the ground plane
                currentForward = Vector3.ProjectOnPlane(currentForward, currentWorldUp).normalized;
                targetForward = Vector3.ProjectOnPlane(targetForward, currentWorldUp).normalized;

                lastRotation = Quaternion.LookRotation(currentForward, currentWorldUp);
                worldRotation = Quaternion.LookRotation(targetForward, currentWorldUp);
            }

            transform.rotation = Quaternion.Slerp(lastRotation, worldRotation, lerp);
        }

        if (_relativeSyncMarker.ApplyRelativePosition && _relativeSyncData.LocalRootPosition.sqrMagnitude < MaxMagnitude)
        {
            Vector3 worldPosition = targetTransform.TransformPoint(targetLocalPosition);
            transform.position = Vector3.Lerp(targetTransform.TransformPoint(_lastSyncData.LocalRootPosition), worldPosition, lerp);
        }

        // negate avatar transform movement
        avatarTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        // fix hip syncing because it is not relative to root, it is synced in world space -_-
        if (hipTrans != null)
        {
            hipTrans.position = transform.position + transform.rotation * relativeHipPos;
            hipTrans.rotation = transform.rotation * relativeHipRot;
        }

        _lastSyncData.LocalRootPosition = targetLocalPosition;
        _lastSyncData.LocalRootRotation = targetLocalRotation.eulerAngles;
    }

    #endregion Unity Events

    #region Public Methods

    public void SetRelativeSyncMarker(RelativeSyncMarker target)
    {
        if (_relativeSyncMarker == target)
            return;

        _relativeSyncMarker = target;

        // calculate relative position and rotation so lerp can smooth it out (hack)
        if (_relativeSyncMarker != null)
        {
            Transform avatarTransform = puppetMaster._animator.transform;
            Transform markerTransform = _relativeSyncMarker.transform;
            Vector3 localPosition = markerTransform.InverseTransformPoint(avatarTransform.position);
            Quaternion localRotation = Quaternion.Inverse(markerTransform.rotation) * avatarTransform.rotation;

            // set last sync data to current position and rotation so we don't lerp from the last marker
            _lastSyncData.LocalRootPosition = localPosition;
            _lastSyncData.LocalRootRotation = localRotation.eulerAngles;
            //Debug.Log($"SetRelativeSyncMarker: {_relativeSyncMarker.name}");
        }
    }

    public void SetRelativePositions(Vector3 position, Vector3 rotation)
    {
        _relativeSyncData.LocalRootPosition = position;
        _relativeSyncData.LocalRootRotation = rotation;
    }

    #endregion Public Methods

    public struct RelativeSyncData
    {
        public Vector3 LocalRootPosition;
        public Vector3 LocalRootRotation;
    }
}