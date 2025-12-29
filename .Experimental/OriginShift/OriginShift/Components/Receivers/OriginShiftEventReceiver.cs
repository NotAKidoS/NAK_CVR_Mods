using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.Events;

#if !UNITY_EDITOR
using ABI_RC.Core.Util;
#endif

namespace NAK.OriginShift.Components;

public class OriginShiftEventReceiver : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] private UnityEvent _onOriginShifted = new();
    [SerializeField] private bool _filterChunkBoundary;
    [SerializeField] private Vector3 _chunkBoundaryMin = Vector3.zero;
    [SerializeField] private Vector3 _chunkBoundaryMax = Vector3.one;

    #endregion Serialized Fields

#if !UNITY_EDITOR
    
    #region Private Fields

    private bool _isInitialized;

    #endregion Private Fields

    #region Unity Events

    private void OnEnable()
    {
            if (!_isInitialized)
            {
                //SharedFilter.SanitizeUnityEvents("OriginShiftEventReceiver", _onOriginShifted);
                
                UnityEventsHelper.SanitizeUnityEvents("OriginShiftEventReceiver", 
                    UnityEventsHelper.EventSource.Unknown, 
                    this, 
                    CVRWorld.Instance, 
                    _onOriginShifted);
                
                _isInitialized = true;
            }

            OriginShiftManager.OnOriginShifted += HandleOriginShifted;
        }

    private void OnDisable()
    {
            OriginShiftManager.OnOriginShifted -= HandleOriginShifted;
        }

    #endregion Unity Events

    #region Origin Shift Events

    private void HandleOriginShifted(Vector3 shift)
    {
            if (_filterChunkBoundary && !IsWithinChunkBoundary(shift))
                return;

            // wrap user-defined event because the user can't be trusted
            try
            {
                _onOriginShifted.Invoke();
            }
            catch (Exception e)
            {
                OriginShiftMod.Logger.Error("OriginShiftEventReceiver: Exception invoking OnOriginShifted event: " + e, this);
            }
        }

    private bool IsWithinChunkBoundary(Vector3 shift)
    {
            return shift.x >= _chunkBoundaryMin.x && shift.x <= _chunkBoundaryMax.x &&
                   shift.y >= _chunkBoundaryMin.y && shift.y <= _chunkBoundaryMax.y &&
                   shift.z >= _chunkBoundaryMin.z && shift.z <= _chunkBoundaryMax.z;
        }

    #endregion Origin Shift Events
    
#endif
}