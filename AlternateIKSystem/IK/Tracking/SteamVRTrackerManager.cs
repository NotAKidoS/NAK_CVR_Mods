using UnityEngine;
using Valve.VR;

namespace AlternateIKSystem.IK.Tracking;

public class SteamVR_TrackerManager : MonoBehaviour
{
    public static SteamVR_TrackerManager Instance { get; private set; }

    private readonly Dictionary<uint, TrackedPoint> trackedPoints = new Dictionary<uint, TrackedPoint>();
    private int lastPosesCount = 0;

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SteamVR_Events.NewPoses.AddListener(OnNewPoses);
    }

    private void OnDisable()
    {
        SteamVR_Events.NewPoses.RemoveListener(OnNewPoses);
    }

    private void OnNewPoses(TrackedDevicePose_t[] poses)
    {
        if (lastPosesCount < poses.Length)
        {
            // If the count has increased, a new tracker has been connected.
            for (uint i = (uint)lastPosesCount; i < poses.Length; i++)
            {
                if (OpenVR.System.GetTrackedDeviceClass(i) == ETrackedDeviceClass.GenericTracker)
                    trackedPoints.Add(i, new TrackedPoint(i));
            }
        }
        else if (lastPosesCount > poses.Length)
        {
            // If the count has decreased, a tracker has been disconnected.
            for (uint i = (uint)poses.Length; i < lastPosesCount; i++)
            {
                if (!trackedPoints.ContainsKey(i))
                    continue;

                trackedPoints[i].Destroy();
                trackedPoints.Remove(i);
            }
        }

        for (uint i = 0; i < poses.Length; i++)
        {
            if (trackedPoints.TryGetValue(i, out TrackedPoint point))
            {
                point.UpdatePose(poses[i]);
            }
        }

        lastPosesCount = poses.Length;
    }

    private class TrackedPoint
    {
        private uint i;

        public TrackedPoint(uint i)
        {
            this.i = i;
        }

        public void UpdatePose(TrackedDevicePose_t pose)
        {

        }

        public void Destroy()
        {

        }
    }
}