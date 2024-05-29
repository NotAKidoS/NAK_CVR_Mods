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
        
        Animator avatarAnimator = PlayerSetup.Instance._animator;
        if (avatarAnimator == null)
            return; // i dont care to bother
        
        RelativeSyncManager.GetRelativeAvatarPositionsFromMarker(
            avatarAnimator, _relativeSyncMarker.transform,
            out Vector3 relativePosition, out Vector3 relativeRotation);
        
        ModNetwork.SetLatestRelativeSync(
            _relativeSyncMarker.pathHash,
            relativePosition, relativeRotation);
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
    
    private void SendEmptyPositionAndRotation()
    {
        ModNetwork.SetLatestRelativeSync(RelativeSyncManager.NoTarget, 
            Vector3.zero, Vector3.zero);
    }
}