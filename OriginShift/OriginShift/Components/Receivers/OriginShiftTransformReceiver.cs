using UnityEngine;

namespace NAK.OriginShift.Components
{
    public class OriginShiftTransformReceiver : MonoBehaviour
    {
#if !UNITY_EDITOR
    
        #region Unity Events

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
    
        private void OnOriginShifted(Vector3 shift)
        {
            transform.position += shift;
        }
    
        #endregion Origin Shift Events
    
#endif
    }
}