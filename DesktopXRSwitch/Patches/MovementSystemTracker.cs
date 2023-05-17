using ABI_RC.Systems.MovementSystem;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class MovementSystemTracker : MonoBehaviour
{
    public MovementSystem movementSystem;
    public Vector3 preSwitchWorldPosition;
    public Quaternion preSwitchWorldRotation;

    void Start()
    {
        movementSystem = GetComponent<MovementSystem>();
        XRModeSwitchTracker.OnPreXRModeSwitch += PreXRModeSwitch;
        XRModeSwitchTracker.OnPostXRModeSwitch += PostXRModeSwitch;
    }

    void OnDestroy()
    {
        XRModeSwitchTracker.OnPreXRModeSwitch -= PreXRModeSwitch;
        XRModeSwitchTracker.OnPostXRModeSwitch -= PostXRModeSwitch;
    }

    public void PreXRModeSwitch(bool isXR, Camera activeCamera)
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

        movementSystem.ChangeCrouch(false);
        movementSystem.ChangeProne(false);
    }

    public void PostXRModeSwitch(bool isXR, Camera activeCamera)
    {
        //immediatly update camera to new camera transform
        movementSystem.rotationPivot = activeCamera.transform;
        //lazy way of correcting Desktop & VR offset issue (game does the maths)
        movementSystem.TeleportToPosRot(preSwitchWorldPosition, preSwitchWorldRotation, false);
        //recenter desktop collision to player object
        if (!isXR) movementSystem.UpdateColliderCenter(movementSystem.transform.position);

        movementSystem.ChangeCrouch(false);
        movementSystem.ChangeProne(false);
    }
}