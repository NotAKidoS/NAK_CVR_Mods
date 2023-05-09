using UnityEngine;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;

namespace NAK.InteractionTest;

internal class GrabbingAvatar : MonoBehaviour
{
    VRIK m_vrik;

    private bool m_isGrabbingLeft = false;
    private bool m_isGrabbingRight = false;

    private Transform m_grabbedTransform = null;
    private Vector3 m_localOffset;
    private Quaternion m_localRotation;

    public float m_grabRadius = 0.1f;
    public LayerMask m_grabLayerMask;

    void Start()
    {
        m_vrik = GetComponent<VRIK>();
    }

    void Update()
    {
        bool isGrabLeft = CVRInputManager.Instance.gripLeftValue >= 0.5f;
        bool isGrabRight = CVRInputManager.Instance.gripRightValue >= 0.5f;

        if (isGrabLeft && !m_isGrabbingLeft)
        {
            m_isGrabbingLeft = true;
            OnGrab(Hand.Left);
        }
        else if (!isGrabLeft && m_isGrabbingLeft)
        {
            m_isGrabbingLeft = false;
            OnRelease(Hand.Left);
        }

        if (isGrabRight && !m_isGrabbingRight)
        {
            m_isGrabbingRight = true;
            OnGrab(Hand.Right);
        }
        else if (!isGrabRight && m_isGrabbingRight)
        {
            m_isGrabbingRight = false;
            OnRelease(Hand.Right);
        }

        if (m_isGrabbingLeft)
        {
            UpdateGrabbedHand(Hand.Left);
        }

        if (m_isGrabbingRight)
        {
            UpdateGrabbedHand(Hand.Right);
        }
    }

    void OnGrab(Hand hand)
    {
        // Find the closest grabbable object using a sphere cast
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_grabRadius, m_grabLayerMask);
        if (colliders.Length > 0)
        {
            Collider closestCollider = colliders[0];
            float closestDistance = float.MaxValue;
            foreach (Collider collider in colliders)
            {
                float distance = (collider.transform.position - transform.position).magnitude;
                if (distance < closestDistance)
                {
                    closestCollider = collider;
                    closestDistance = distance;
                }
            }

            // Cache the grabbed transform and local position/rotation offset
            m_grabbedTransform = closestCollider.transform;
            m_localOffset = transform.InverseTransformVector(m_grabbedTransform.position - transform.position);
            m_localRotation = Quaternion.Inverse(transform.rotation) * m_grabbedTransform.rotation;
        }
    }

    void OnRelease(Hand hand)
    {
        // Reset the grabbed transform and local position/rotation offset
        m_grabbedTransform = null;
        m_localOffset = Vector3.zero;
        m_localRotation = Quaternion.identity;
    }

    void UpdateGrabbedHand(Hand hand)
    {

    }
}

public enum Hand
{
    Left,
    Right
}
