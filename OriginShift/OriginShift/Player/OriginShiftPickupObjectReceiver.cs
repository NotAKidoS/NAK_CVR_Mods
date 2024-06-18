using ABI.CCK.Components;
using UnityEngine;

namespace NAK.OriginShift;

public class OriginShiftPickupObjectReceiver : MonoBehaviour
{
    private CVRPickupObject _pickupObject;

    #region Unity Events

    private void Start()
    {
        _pickupObject = GetComponent<CVRPickupObject>();
        if (_pickupObject == null)
        {
            OriginShiftMod.Logger.Error("OriginShiftPickupObjectReceiver requires a CVRPickupObject component!");
            enabled = false;
            return;
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
        _pickupObject._respawnHeight += shift.y;
    }
    
    #endregion Origin Shift Events
}