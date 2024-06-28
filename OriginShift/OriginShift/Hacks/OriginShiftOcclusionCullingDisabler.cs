using UnityEngine;

namespace NAK.OriginShift.Hacks
{
    public class OriginShiftOcclusionCullingDisabler : MonoBehaviour
    {
        private Camera _camera;
        private bool _originalCullingState;
        
        #region Unity Events

        private void Start()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("OriginShiftOcclusionCullingDisabler requires a Camera component on the same GameObject.");
                enabled = false;
                return;
            }
            _originalCullingState = _camera.useOcclusionCulling;
        }

        private void Awake() // we want to execute even if the component is disabled
        {
            OriginShiftManager.OnStateChanged += OnOriginShiftStateChanged;
        }
    
        private void OnDestroy()
        {
            OriginShiftManager.OnStateChanged -= OnOriginShiftStateChanged;
        }

        #endregion Unity Events
    
        #region Origin Shift Events
    
        private void OnOriginShiftStateChanged(OriginShiftManager.OriginShiftState state)
        {
            _camera.useOcclusionCulling = state != OriginShiftManager.OriginShiftState.Forced && _originalCullingState;
        }
    
        #endregion Origin Shift Events
    }
}