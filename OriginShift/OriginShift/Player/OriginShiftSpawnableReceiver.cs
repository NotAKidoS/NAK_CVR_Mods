using ABI.CCK.Components;
using UnityEngine;

namespace NAK.OriginShift;

public class OriginShiftSpawnableReceiver : MonoBehaviour
{
    private CVRSpawnable _spawnable;

    #region Unity Events

    private void Start()
    {
        _spawnable = GetComponent<CVRSpawnable>();
        if (_spawnable == null)
        {
            OriginShiftMod.Logger.Error("OriginShiftSpawnableReceiver: No CVRSpawnable found on GameObject: " + gameObject.name, this);
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
        _spawnable.futurePosition += shift;
        _spawnable.currentPosition += shift;
        _spawnable.pastPosition += shift; // not used by game, just cached ?
    }
    
    #endregion Origin Shift Events
}