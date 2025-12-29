using UnityEngine;

namespace NAK.OriginShift.Components;

public class OriginShiftTrailRendererReceiver : MonoBehaviour
{
#if !UNITY_EDITOR
    
    // max positions count cause i said so
    private static readonly Vector3[] _tempPositions = new Vector3[10000];
    
    private TrailRenderer[] _trailRenderers;
    
    #region Unity Events
    
    private void Start()
    {
            _trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
            if (_trailRenderers.Length == 0)
            {
                // OriginShiftMod.Logger.Error("OriginShiftTrailRendererReceiver: No TrailRenderers found on GameObject: " + gameObject.name, this);
                enabled = false;
            }
        }
    
    private void OnEnable()
    {
            OriginShiftManager.OnOriginShifted += OnOriginShifted;
        }

    private void OnDisable()
    {
            OriginShiftManager.OnOriginShifted -= OnOriginShifted;
        }
    
    #endregion Unity Events
    
    #region Origin Shift Events

    private void OnOriginShifted(Vector3 offset)
    {
            foreach (TrailRenderer trailRenderer in _trailRenderers)
                ShiftTrailRenderer(trailRenderer, offset);
        }

    private static void ShiftTrailRenderer(TrailRenderer trailRenderer, Vector3 offset)
    {
            trailRenderer.GetPositions(_tempPositions);
            for (var i = 0; i < _tempPositions.Length; i++) _tempPositions[i] += offset;
            trailRenderer.SetPositions(_tempPositions);
        }
    
    #endregion Origin Shift Events
    
#endif
}