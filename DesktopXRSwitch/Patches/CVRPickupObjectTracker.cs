using ABI.CCK.Components;
using UnityEngine;

//Thanks Ben! I was scared of transpiler so I reworked a bit...

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class CVRPickupObjectTracker : MonoBehaviour
{
    public CVRPickupObject pickupObject;
    public Transform storedGripOrigin;

    void Start()
    {
        XRModeSwitchTracker.OnPostXRModeSwitch += PostXRModeSwitch;
    }

    void OnDestroy()
    {
        XRModeSwitchTracker.OnPostXRModeSwitch -= PostXRModeSwitch;
    }

    public void PostXRModeSwitch(bool isXR, Camera activeCamera)
    {
        if (pickupObject != null)
        {
            if (pickupObject._controllerRay != null) pickupObject._controllerRay.DropObject(true);
            (storedGripOrigin, pickupObject.gripOrigin) = (pickupObject.gripOrigin, storedGripOrigin);
        }
    }
}