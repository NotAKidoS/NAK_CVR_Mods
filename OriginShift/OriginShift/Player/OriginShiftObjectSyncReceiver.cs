using ABI.CCK.Components;
using UnityEngine;

namespace NAK.OriginShift;

public class OriginShiftObjectSyncReceiver : MonoBehaviour
{
    private CVRObjectSync _objectSync;
    
    #region Unity Events
    
    private void Start()
    {
        _objectSync = GetComponent<CVRObjectSync>();
        if (_objectSync == null)
        {
            OriginShiftMod.Logger.Error("OriginShiftObjectSyncReceiver: No CVRObjectSync found on GameObject: " + gameObject.name, this);
            enabled = false;
        }
        OriginShiftManager.OnOriginShifted += OnOriginShifted;
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
        // idc, just shift all of them
        _objectSync._oldData.position += shift;
        _objectSync._currData.position += shift;
        _objectSync._futureData.position += shift;
    }
    
    #endregion Origin Shift Events
}