using ABI.CCK.Components;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRPickupObjectTracker : MonoBehaviour
{
    internal CVRPickupObject _pickupObject;
    internal Transform _storedGripOrigin;

    private void Start()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    private void OnDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    public void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        if (_pickupObject == null) 
            return;
        
        // Drop the object if it is being held locally
        if (_pickupObject._controllerRay != null)
            _pickupObject._controllerRay.DropObject(true);

        // Swap the grip origins
        (_storedGripOrigin, _pickupObject.gripOrigin) = (_pickupObject.gripOrigin, _storedGripOrigin);
    }
}
