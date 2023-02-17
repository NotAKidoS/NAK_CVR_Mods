using ABI_RC.Systems.MovementSystem;
using UnityEngine;

namespace NAK.Melons.DesktopVRSwitch.Patches;

public class MovementSystemTracker : MonoBehaviour
{
    public MovementSystem movementSystem;

    public Vector3 preSwitchWorldPosition;
    public Quaternion preSwitchWorldRotation;

    void Start()
    {
        movementSystem = GetComponent<MovementSystem>();
        VRModeSwitchTracker.OnPostVRModeSwitch += PreVRModeSwitch;
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }

    void OnDestroy()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch -= PreVRModeSwitch;
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PreVRModeSwitch(bool enterXR, Camera activeCamera)
    {
        preSwitchWorldPosition = movementSystem.rotationPivot.transform.position;
        preSwitchWorldRotation = movementSystem.rotationPivot.transform.rotation;
    }

    public void PostVRModeSwitch(bool enterXR, Camera activeCamera)
    {
        //lazy way of correcting Desktop & VR offset issue (game does the maths)
        movementSystem.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);
        //recenter desktop collision to player object
        if (!enterXR) movementSystem.UpdateColliderCenter(movementSystem.transform.position);
    }
}