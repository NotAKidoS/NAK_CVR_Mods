using UnityEngine;

namespace NAK.DesktopVRIK.IK.VRIKHelpers;

public struct VRIKLocomotionData
{
    public Vector3 InitialFootPosLeft;
    public Vector3 InitialFootPosRight;
    public Quaternion InitialFootRotLeft;
    public Quaternion InitialFootRotRight;
    public float InitialFootDistance;
    public float InitialStepThreshold;
    public float InitialStepHeight;

    public void Clear()
    {
        InitialFootPosLeft = Vector3.zero;
        InitialFootPosRight = Vector3.zero;
        InitialFootRotLeft = Quaternion.identity;
        InitialFootRotRight = Quaternion.identity;
        InitialFootDistance = 0f;
        InitialStepThreshold = 0f;
        InitialStepHeight = 0f;
    }
}