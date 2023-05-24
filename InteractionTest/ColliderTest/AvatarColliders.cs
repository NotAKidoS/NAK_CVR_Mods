using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarColliders : MonoBehaviour
{
    public Animator animator;
    public AvatarTransforms m_avatarTransforms;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        m_avatarTransforms = m_avatarTransforms.GetAvatarTransforms(animator);

        CreateColliderTorso();
        CreateColliderLeftArm();
        CreateColliderRightArm();
    }

    void CreateColliderLeftArm()
    {
        // Calculate the magnitudes for the upper arm, lower arm, and hand.
        // This would need to be adjusted based on the specific avatar model being used.
        float upperArmMagnitude = CalculateMagnitude(m_avatarTransforms.leftUpperArm.position, m_avatarTransforms.leftLowerArm.position);
        //float lowerArmMagnitude = CalculateMagnitude(m_avatarTransforms.leftLowerArm.position, m_avatarTransforms.leftHand.position);

        float handMagnitude = upperArmMagnitude * 0.25f; // Assuming hand is about half the size of the upper arm.

        // Create a collider for the upper arm area
        Vector3 upperArmPosition = m_avatarTransforms.leftUpperArm.position;
        Vector3 lowerArmPosition = m_avatarTransforms.leftLowerArm.position;
        CreateCollider(m_avatarTransforms.leftUpperArm, upperArmPosition, lowerArmPosition, handMagnitude, 0.15f);

        // Create a collider for the lower arm area
        Vector3 handPosition = m_avatarTransforms.leftHand.position;
        CreateCollider(m_avatarTransforms.leftLowerArm, lowerArmPosition, handPosition, handMagnitude, 0.15f);

        // Create a collider for the hand area
        // For simplicity, let's assume the end position is slightly offset from the hand position.
        Vector3 handEndPosition = handPosition + (handPosition - lowerArmPosition) * 0.5f;
        CreateCollider(m_avatarTransforms.leftHand, handPosition, handEndPosition, handMagnitude, 0.15f);
    }

    void CreateColliderRightArm()
    {
        // Calculate the magnitudes for the upper arm, lower arm, and hand.
        // This would need to be adjusted based on the specific avatar model being used.
        float upperArmMagnitude = CalculateMagnitude(m_avatarTransforms.rightUpperArm.position, m_avatarTransforms.rightLowerArm.position);
        //float lowerArmMagnitude = CalculateMagnitude(m_avatarTransforms.rightLowerArm.position, m_avatarTransforms.rightHand.position);

        float handMagnitude = upperArmMagnitude * 0.25f; // Assuming hand is about half the size of the upper arm.

        // Create a collider for the upper arm area
        Vector3 upperArmPosition = m_avatarTransforms.rightUpperArm.position;
        Vector3 lowerArmPosition = m_avatarTransforms.rightLowerArm.position;
        CreateCollider(m_avatarTransforms.rightUpperArm, upperArmPosition, lowerArmPosition, handMagnitude, 0.15f);

        // Create a collider for the lower arm area
        Vector3 handPosition = m_avatarTransforms.rightHand.position;
        CreateCollider(m_avatarTransforms.rightLowerArm, lowerArmPosition, handPosition, handMagnitude, 0.15f);

        // Create a collider for the hand area
        // For simplicity, let's assume the end position is slightly offset from the hand position.
        Vector3 handEndPosition = handPosition + (handPosition - lowerArmPosition) * 0.5f;
        CreateCollider(m_avatarTransforms.rightHand, handPosition, handEndPosition, handMagnitude, 0.15f);
    }

    void CreateColliderTorso()
    {
        float legMagnitude = CalculateMagnitude(m_avatarTransforms.rightUpperLeg.position, m_avatarTransforms.leftUpperLeg.position);

        //gets between upperarm and shoulder distances
        float armMagnitude = CalculateMagnitude(m_avatarTransforms.rightUpperArm.position, m_avatarTransforms.leftShoulder.position);

        Vector3 hipsPosition = GetHipsPosition();

        // Create a collider for the hips area
        Vector3 spinePosition = m_avatarTransforms.spine.position;
        CreateCollider(m_avatarTransforms.hips, hipsPosition, spinePosition, legMagnitude, 0.15f);

        // Create a collider for the chest area
        Vector3 chestPosition = m_avatarTransforms.chest.position;
        CreateCollider(m_avatarTransforms.spine, spinePosition, chestPosition, (legMagnitude + armMagnitude) / 2, 0.15f);

        // Create a collider for the neck area
        Vector3 neckPosition = m_avatarTransforms.neck.position;
        CreateCollider(m_avatarTransforms.chest, chestPosition, neckPosition, armMagnitude, 0.15f);
    }

    static float CalculateMagnitude(Vector3 position1, Vector3 position2)
    {
        return (position1 - position2).magnitude;
    }

    Vector3 GetHipsPosition()
    {
        Vector3 hipsPosition = m_avatarTransforms.hips.position;

        if (Vector3.Distance(hipsPosition, m_avatarTransforms.root.position) < Vector3.Distance(m_avatarTransforms.head.position, m_avatarTransforms.root.position) * 0.25f)
        {
            hipsPosition = Vector3.Lerp(m_avatarTransforms.leftUpperLeg.position, m_avatarTransforms.rightUpperLeg.position, 0.5f);
        }

        return hipsPosition;
    }

    static void CreateCollider(Transform root, Vector3 start, Vector3 end, float width, float overlap)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude * (1f + overlap);
        Vector3 axisVectorToDirection = GetAxisVectorToDirection(root.rotation, direction);
        
        Vector3 lossyScale = root.lossyScale;
        float scaleF = (lossyScale.x + lossyScale.y + lossyScale.z) / 3f;

        CapsuleCollider capsuleCollider = root.gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.height = Mathf.Abs(length / scaleF);
        capsuleCollider.radius = Mathf.Abs(width / scaleF);
        capsuleCollider.direction = DirectionVector3ToInt(axisVectorToDirection);
        capsuleCollider.center = root.InverseTransformPoint(Vector3.Lerp(start, end, 0.5f));
        capsuleCollider.isTrigger = true;
    }

    public static Vector3 GetAxisVectorToDirection(Quaternion rotation, Vector3 direction)
    {
        direction = direction.normalized;
        Vector3 result = Vector3.right;

        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;

        float dotRight = Mathf.Abs(Vector3.Dot(Vector3.Normalize(right), direction));
        float dotUp = Mathf.Abs(Vector3.Dot(Vector3.Normalize(up), direction));
        float dotForward = Mathf.Abs(Vector3.Dot(Vector3.Normalize(forward), direction));

        if (dotUp > dotRight)
        {
            result = Vector3.up;
        }

        if (dotForward > dotRight && dotForward > dotUp)
        {
            result = Vector3.forward;
        }

        return result;
    }
    
    public static int DirectionVector3ToInt(Vector3 dir)
    {
        float dotRight = Vector3.Dot(dir, Vector3.right);
        float dotUp = Vector3.Dot(dir, Vector3.up);
        float dotForward = Vector3.Dot(dir, Vector3.forward);

        float absDotRight = Mathf.Abs(dotRight);
        float absDotUp = Mathf.Abs(dotUp);
        float absDotForward = Mathf.Abs(dotForward);

        int result = 0;

        if (absDotUp > absDotRight && absDotUp > absDotForward)
        {
            result = 1;
        }
        else if (absDotForward > absDotRight && absDotForward > absDotUp)
        {
            result = 2;
        }

        return result;
    }
}
