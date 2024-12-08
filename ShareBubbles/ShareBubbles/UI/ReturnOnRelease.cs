using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace NAK.ShareBubbles.UI;

public class ReturnOnRelease : MonoBehaviour
{
    public float returnSpeed = 10.0f;
    public float damping = 0.5f;

    private bool isReturning;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;
    
    private Pickupable pickupable;

    private void Start()
    {
        Transform ourTransform = transform;
        originalLocalPosition = ourTransform.localPosition;
        originalLocalRotation = ourTransform.localRotation;
        
        // TODO: Instead of LateUpdate, use OnDrop to start a coroutine
        pickupable = GetComponent<Pickupable>();
        pickupable.onGrab.AddListener(OnPickupGrabbed);
        pickupable.onDrop.AddListener(OnPickupRelease);
    }

    public void OnPickupGrabbed()
    {
        isReturning = false;
    }

    public void OnPickupRelease()
    {
        isReturning = true;
    }

    private void LateUpdate()
    {
        if (isReturning)
        {
            // Smoothly damp position back to the original
            Transform ourTransform = transform;
            transform.localPosition = Vector3.SmoothDamp(
                ourTransform.localPosition,
                originalLocalPosition,
                ref positionVelocity,
                damping,
                returnSpeed
            );

            // Smoothly damp rotation back to the original
            transform.localRotation = SmoothDampQuaternion(
                ourTransform.localRotation,
                originalLocalRotation,
                ref angularVelocity,
                damping
            );

            // Stop returning when close enough
            if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.01f 
                && angularVelocity.magnitude < 0.01f)
            {
                isReturning = false;
                transform.SetLocalPositionAndRotation(originalLocalPosition, originalLocalRotation);
            }
        }
    }

    private Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 velocity, float smoothTime)
    {
        // Decompose rotation to Euler angles for smoother interpolation
        Vector3 currentEuler = current.eulerAngles;
        Vector3 targetEuler = target.eulerAngles;

        // Perform SmoothDamp on each axis
        Vector3 smoothedEuler = new Vector3(
            Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref velocity.x, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref velocity.y, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref velocity.z, smoothTime)
        );

        // Convert back to Quaternion
        return Quaternion.Euler(smoothedEuler);
    }
}