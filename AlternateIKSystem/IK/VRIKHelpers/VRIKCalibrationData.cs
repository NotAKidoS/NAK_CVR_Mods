using UnityEngine;

namespace NAK.AlternateIKSystem.VRIKHelpers;

public struct VRIKCalibrationData
{
    public Vector3 KneeNormalLeft;
    public Vector3 KneeNormalRight;
    public Vector3 InitialFootPosLeft;
    public Vector3 InitialFootPosRight;
    public Quaternion InitialFootRotLeft;
    public Quaternion InitialFootRotRight;
    public float InitialHeadHeight;
    public float InitialFootDistance;
    public float InitialStepThreshold;
    public float InitialStepHeight;

    public void Clear()
    {
        KneeNormalLeft = Vector3.zero;
        KneeNormalRight = Vector3.zero;
        InitialFootPosLeft = Vector3.zero;
        InitialFootPosRight = Vector3.zero;
        InitialFootRotLeft = Quaternion.identity;
        InitialFootRotRight = Quaternion.identity;
        InitialHeadHeight = 0f;
        InitialFootDistance = 0f;
        InitialStepThreshold = 0f;
        InitialStepHeight = 0f;
    }
}