using ABI.CCK.Components;
using UnityEngine;

//Thanks Ben! I was scared of transpiler so I reworked a bit...

namespace NAK.DesktopVRSwitch.Patches;

public class CVRPickupObjectTracker : MonoBehaviour
{
    public CVRPickupObject pickupObject;
    public Transform storedGripOrigin;

    void Start()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }

    void OnDestroy()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PostVRModeSwitch(bool isVR, Camera activeCamera)
    {
        if (pickupObject != null)
        {
            if (pickupObject._controllerRay != null) pickupObject._controllerRay.DropObject(true);
            (storedGripOrigin, pickupObject.gripOrigin) = (pickupObject.gripOrigin, storedGripOrigin);
        }
    }
}
