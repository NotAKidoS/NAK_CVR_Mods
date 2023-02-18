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

    public void PreVRModeSwitch(bool enterVR, Camera activeCamera)
    {
        //correct rotationPivot y position, so we dont teleport up/down
        Vector3 position = movementSystem.rotationPivot.transform.position;
        position.y = movementSystem.transform.position.y;
        preSwitchWorldPosition = position;
        preSwitchWorldRotation = movementSystem.rotationPivot.transform.rotation;
        //ChilloutVR does not use VRIK root right, so avatar root is VR player root.
        //This causes desync between VR and Desktop positions & collision on switch.

        //I correct for this in lazy way, but i use rotationPivot instead of avatar root,
        //so the user can still switch even if avatar is null (if it failed to load for example).
    }

    public void PostVRModeSwitch(bool enterVR, Camera activeCamera)
    {
        //lazy way of correcting Desktop & VR offset issue (game does the maths)
        movementSystem.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);
        //recenter desktop collision to player object
        if (!enterVR) movementSystem.UpdateColliderCenter(movementSystem.transform.position);
    }
}