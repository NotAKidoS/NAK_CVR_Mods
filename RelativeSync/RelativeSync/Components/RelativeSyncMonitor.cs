using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using NAK.RelativeSync.Networking;
using UnityEngine;

namespace NAK.RelativeSync.Components;

[DefaultExecutionOrder(int.MaxValue)]
public class RelativeSyncMonitor : MonoBehaviour
{
    private BetterBetterCharacterController _characterController { get; set; }
        
    private RelativeSyncMarker _relativeSyncMarker;
    private RelativeSyncMarker _lastRelativeSyncMarker;

    private void Start()
    {
        _characterController = GetComponent<BetterBetterCharacterController>();
    }

    private void LateUpdate()
    {
        if (_characterController == null)
            return;
            
        CheckForRelativeSyncMarker();

        if (_relativeSyncMarker == null)
        {
            if (_lastRelativeSyncMarker == null) 
                return;
                
            // send empty position and rotation to stop syncing
            SendEmptyPositionAndRotation();
            _lastRelativeSyncMarker = null;
            return;
        }

        _lastRelativeSyncMarker = _relativeSyncMarker;
            
        SendCurrentPositionAndRotation();
    }

    private void CheckForRelativeSyncMarker()
    {
        if (_characterController._isSitting && _characterController._lastCvrSeat)
        {
            RelativeSyncMarker newMarker = _characterController._lastCvrSeat.GetComponent<RelativeSyncMarker>();
            _relativeSyncMarker = newMarker;
            return;
        }
            
        if (_characterController._previousMovementParent != null)
        {
            RelativeSyncMarker newMarker = _characterController._previousMovementParent.GetComponent<RelativeSyncMarker>();
            _relativeSyncMarker = newMarker;
            return;
        }
            
        // none found
        _relativeSyncMarker = null;
    }

    private void SendCurrentPositionAndRotation()
    {
        // because our syncing is retarded, we need to sync relative from the avatar root...
        Transform avatarRoot = PlayerSetup.Instance._avatar.transform;
        Vector3 avatarRootPosition = avatarRoot.position; // PlayerSetup.Instance.GetPlayerPosition()
        Quaternion avatarRootRotation = avatarRoot.rotation; // PlayerSetup.Instance.GetPlayerRotation()
            
        Transform markerTransform = _relativeSyncMarker.transform;
        Vector3 localPosition = markerTransform.InverseTransformPoint(avatarRootPosition);
        Quaternion localRotation = Quaternion.Inverse(markerTransform.rotation) * avatarRootRotation;
            
        ModNetwork.SendNetworkPosition(_relativeSyncMarker.pathHash, localPosition, localRotation.eulerAngles);
    }
        
    private void SendEmptyPositionAndRotation()
    {
        ModNetwork.SendNetworkPosition(RelativeSyncManager.NoTarget, Vector3.zero, Vector3.zero);
    }
}