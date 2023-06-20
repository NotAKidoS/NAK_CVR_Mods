using ABI.CCK.Components;
using UnityEngine;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class CVRPickupObjectTracker : MonoBehaviour
{
    private CVRPickupObject _pickupObject;
    private Transform _storedGripOrigin;

    public CVRPickupObjectTracker(CVRPickupObject pickupObject, Transform storedGripOrigin)
    {
        this._pickupObject = pickupObject;
        this._storedGripOrigin = storedGripOrigin;
    }

    private void OnDestroy()
    {

    }

    public void OnPostSwitch(bool intoVR)
    {
        if (_pickupObject != null)
        {
            // Drop the object if it is being held locally
            if (_pickupObject._controllerRay != null)
                _pickupObject._controllerRay.DropObject(true);

            // Swap the grip origins
            (_storedGripOrigin, _pickupObject.gripOrigin) = (_pickupObject.gripOrigin, _storedGripOrigin);
        }
    }
}