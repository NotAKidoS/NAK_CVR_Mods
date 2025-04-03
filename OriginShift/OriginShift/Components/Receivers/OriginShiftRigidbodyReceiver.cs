using UnityEngine;

namespace NAK.OriginShift.Components;

public class OriginShiftRigidbodyReceiver : MonoBehaviour
{
#if !UNITY_EDITOR
    
    private Rigidbody _rigidbody;
    
    #region Unity Events

    private void Start()
    {
            _rigidbody = GetComponentInChildren<Rigidbody>();
            if (_rigidbody == null)
            {
                OriginShiftMod.Logger.Error("OriginShiftRigidbodyReceiver: No Rigidbody found on GameObject: " + gameObject.name, this);
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
    
    private void OnOriginShifted(Vector3 shift)
    {
            _rigidbody.position += shift;
        }
    
    #endregion Origin Shift Events
    
#endif
}