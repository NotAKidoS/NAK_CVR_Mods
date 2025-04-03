using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.OriginShift;

public class OriginShiftNetIkReceiver : MonoBehaviour
{
    private PuppetMaster _puppetMaster;
    
    #region Unity Events

    private void Start()
    {
        _puppetMaster = GetComponent<PuppetMaster>();
        if (_puppetMaster == null)
        {
            OriginShiftMod.Logger.Error("OriginShiftNetIkReceiver: No PuppetMaster found on GameObject: " + gameObject.name, this);
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
        // would be nice if these were relative positions like sub-syncs :)
        _puppetMaster._playerAvatarMovementDataCurrent.RootPosition += shift;
        _puppetMaster._playerAvatarMovementDataCurrent.BodyPosition += shift;
        _puppetMaster._playerAvatarMovementDataPast.RootPosition += shift;
        _puppetMaster._playerAvatarMovementDataPast.BodyPosition += shift;
        // later in frame puppetmaster will update remote player position
    }

    #endregion Origin Shift Events
}