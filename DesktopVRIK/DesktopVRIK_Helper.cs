using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using RootMotion.FinalIK;

namespace NAK.Melons.DesktopVRIK;

internal class DesktopVRIK_Helper : MonoBehaviour
{
    public static DesktopVRIK_Helper Instance;

    //Avatar
    public Transform avatar_HeadBone;

    //DesktopVRIK
    public Transform ik_HeadFollower;
    public Quaternion ik_HeadRotation;

    public static void CreateInstance()
    {
        Transform helper = new GameObject("[DesktopVRIK] Virtual Rig").transform;
        helper.parent = PlayerSetup.Instance.transform;
        helper.localPosition = Vector3.zero;
        helper.localRotation = Quaternion.identity;
        helper.gameObject.AddComponent<DesktopVRIK_Helper>();
    }

    void Start()
    {
        Instance = this;

        Transform headFollower = new GameObject("HeadBone_Follower").transform;
        headFollower.parent = transform;
        headFollower.localPosition = new Vector3(0f, 1.8f, 0f);
        headFollower.localRotation = Quaternion.identity;
        ik_HeadFollower = headFollower;
    }

    public void OnUpdateVRIK()
    {
        if (avatar_HeadBone != null)
        {
            float globalWeight = (1 - MovementSystem.Instance.movementVector.magnitude);
            globalWeight *= IKSystem.vrik.solver.locomotion.weight;

            //the most important thing ever
            IKSystem.vrik.solver.spine.rotationWeight = globalWeight;

            HeadIK_FollowPosition();

            HeadIK_RotateWithWeight(globalWeight);
            HeadIK_FollowWithinAngle(globalWeight);
            ik_HeadFollower.rotation = ik_HeadRotation;
        }
    }

    public void OnResetIK()
    {
        ik_HeadRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
    }

    public void HeadIK_FollowPosition()
    {
        ik_HeadFollower.position = new Vector3(transform.position.x, avatar_HeadBone.position.y, transform.position.z);
    }

    public void HeadIK_FollowWithinAngle(float weight)
    {
        if (DesktopVRIK.Setting_BodyAngleLimit != 0)
        {
            float weightedAngle = DesktopVRIK.Setting_BodyAngleLimit * weight;
            float currentAngle = Mathf.DeltaAngle(transform.eulerAngles.y, ik_HeadRotation.eulerAngles.y);
            if (Mathf.Abs(currentAngle) > weightedAngle)
            {
                float fixedCurrentAngle = currentAngle > 0 ? currentAngle : -currentAngle;
                float clampedAngle = Mathf.MoveTowardsAngle(ik_HeadRotation.eulerAngles.y, transform.eulerAngles.y, fixedCurrentAngle - weightedAngle);
                ik_HeadRotation = Quaternion.Euler(ik_HeadRotation.eulerAngles.x, clampedAngle, 0);
            }
        }
        else
        {
            ik_HeadRotation = Quaternion.Euler(ik_HeadRotation.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
        }
    }

    public void HeadIK_RotateWithWeight(float weight)
    {
        //VRChat hip movement emulation
        if (DesktopVRIK.Setting_BodyLeanWeight != 0)
        {
            float angle = PlayerSetup.Instance.desktopCamera.transform.localEulerAngles.x;
            if (angle > 180) angle -= 360;
            float leanAmount = angle * weight * DesktopVRIK.Setting_BodyLeanWeight;
            ik_HeadRotation = Quaternion.Euler(leanAmount * 0.33f, ik_HeadRotation.eulerAngles.y, 0);
        }
        else
        {
            ik_HeadRotation = Quaternion.Euler(transform.eulerAngles.x, ik_HeadRotation.eulerAngles.y, transform.eulerAngles.z);
        }
    }
}