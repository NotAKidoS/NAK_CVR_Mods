using ABI.CCK.Components;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.CCK.CustomComponents;

public class NAKPointerTracker : MonoBehaviour
{
    // Configuration
    public Transform referenceTransform;
    public string pointerType = "";
    public float radius = 0.1f;
    public Vector3 offset = Vector3.zero;

    // Animator module
    public Animator animator;
    public string parameterName;

    // Internal stuff
    bool isLocal;
    float initialAngle;
    CVRPointer trackedPointer;

    void Start()
    {
        // Create collider
        Collider collider = base.gameObject.GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphereCollider = base.gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            Vector3 lossyScale = base.transform.lossyScale;
            sphereCollider.radius = radius / Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
            sphereCollider.center = offset;
        }

        // Create rigidbody (required for triggers)
        Rigidbody rigidbody = base.gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = base.gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }

        // Initial setup
        if (referenceTransform == null) referenceTransform = transform;
        Vector3 direction = (transform.TransformPoint(offset) - referenceTransform.position);
        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, referenceTransform.up);
        initialAngle = Vector3.SignedAngle(referenceTransform.forward, projectedDirection, referenceTransform.up);
        isLocal = gameObject.layer == 8;
    }

    void OnDrawGizmosSelected()
    {
        if (base.isActiveAndEnabled)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
            Gizmos.DrawWireSphere(offset, radius);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (trackedPointer != null) return;

        // generic pointer or specific pointer
        CVRPointer pointer = other.gameObject.GetComponent<CVRPointer>();
        if (pointer != null && (String.IsNullOrEmpty(pointerType) || pointer.type == pointerType))
        {
            trackedPointer = pointer;
        }
    }

    void Update()
    {
        if (trackedPointer == null) return;

        // Check if tracked pointer was disabled
        if (!trackedPointer.isActiveAndEnabled)
        {
            ReleasePointer();
            return;
        }

        TrackPointer();
    }

    void TrackPointer()
    {
        if (animator != null)
        {
            float angle = GetAngleFromPosition(trackedPointer.transform.position, initialAngle) / 360;
            if (!isLocal)
            {
                animator.SetFloat(parameterName + "_Angle", angle);
                return;
            }
            PlayerSetup.Instance.changeAnimatorParam(parameterName + "_Angle", angle);
        }
    }

    void ReleasePointer()
    {
        trackedPointer = null;
    }

    float GetAngleFromPosition(Vector3 trackedPos, float offset = 0)
    {
        Vector3 direction = (trackedPos - referenceTransform.position);
        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, referenceTransform.up);
        float angle = Vector3.SignedAngle(referenceTransform.forward, projectedDirection, referenceTransform.up) - offset;
        if (angle < 0) angle += 360f;
        return angle;
    }
}
