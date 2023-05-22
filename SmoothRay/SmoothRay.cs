using ABI_RC.Core.InteractionSystem;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

namespace NAK.SmoothRay;

public class SmoothRayer : MonoBehaviour
{
    internal ControllerRay ray;

    //settings
    bool isEnabled;
    bool menuOnly;
    float positionSmoothingValue;
    float rotationSmoothingValue;
    float smallMovementThresholdAngle;

    //internal
    Vector3 smoothedPosition = Vector3.zero;
    Quaternion smoothedRotation = Quaternion.identity;
    float angleVelocitySnap = 1f;

    //native & trackedcontrollerfix stuff
    SteamVR_Behaviour_Pose pose;
    SteamVR_TrackedObject tracked;
    SteamVR_Events.Action newPosesAction = null;
    
    void Start()
    {
        // native CVR
        pose = GetComponent<SteamVR_Behaviour_Pose>();
        if (pose != null)
            pose.onTransformUpdatedEvent += OnTransformUpdated;

        // trackedcontrollerfix support
        tracked = GetComponent<SteamVR_TrackedObject>();
        if (tracked != null)
        {
            newPosesAction = SteamVR_Events.NewPosesAppliedAction(new UnityAction(OnAppliedPoses));
            newPosesAction.enabled = true;
        }

        foreach (var setting in SmoothRay.Category.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        OnUpdateSettings(null, null);
    }
    
    void OnEnable()
    {
        smoothedPosition = transform.localPosition;
        smoothedRotation = transform.localRotation;

        // desktopvrswitch support, start handles this for normal use
        if (pose != null)
            pose.onTransformUpdatedEvent += OnTransformUpdated;
        if (tracked != null && newPosesAction != null)
        {
            newPosesAction.enabled = true;
        }
    }

    void OnDisable()
    {
        smoothedPosition = transform.localPosition;
        smoothedRotation = transform.localRotation;

        // desktopvrswitch support, normal use wont run this
        if (pose != null)
            pose.onTransformUpdatedEvent -= OnTransformUpdated;
        if (tracked != null && newPosesAction != null)
            newPosesAction.enabled = false;
    }

    void OnUpdateSettings(object arg1, object arg2)
    {
        isEnabled = SmoothRay.EntryEnabled.Value;
        menuOnly = SmoothRay.EntryMenuOnly.Value;
        smallMovementThresholdAngle = SmoothRay.EntrySmallMovementThresholdAngle.Value;
        // dont let value hit 0, itll freeze controllers
        positionSmoothingValue = Math.Max(20f - Mathf.Clamp(SmoothRay.EntryPositionSmoothing.Value, 0f, 20f), 0.1f);
        rotationSmoothingValue = Math.Max(20f - Mathf.Clamp(SmoothRay.EntryRotationSmoothing.Value, 0f, 20f), 0.1f);
    }

    void OnAppliedPoses() => SmoothTransform();
    void OnTransformUpdated(SteamVR_Behaviour_Pose pose, SteamVR_Input_Sources inputSource) => SmoothTransform();
    
    void SmoothTransform()
    {
        if (isEnabled && ray.lineRenderer != null && ray.lineRenderer.enabled)
        {
            if (menuOnly && (!ray.uiActive || (ray.hitTransform != ViewManager.Instance.transform && ray.hitTransform != CVR_MenuManager.Instance.quickMenu.transform)))
            {
                return;
            }

            var angDiff = Quaternion.Angle(smoothedRotation, transform.localRotation);
            angleVelocitySnap = Mathf.Min(angleVelocitySnap + angDiff, 90f);

            var snapMulti = Mathf.Clamp(angleVelocitySnap / smallMovementThresholdAngle, 0.1f, 2.5f);

            if (angleVelocitySnap > 0.1f)
                angleVelocitySnap -= Mathf.Max(0.4f, angleVelocitySnap / 1.7f);

            if (positionSmoothingValue < 20f)
            {
                smoothedPosition = Vector3.Lerp(smoothedPosition, transform.localPosition, positionSmoothingValue * Time.deltaTime * snapMulti);
                transform.localPosition = smoothedPosition;
            }

            if (rotationSmoothingValue < 20f)
            {
                smoothedRotation = Quaternion.Lerp(smoothedRotation, transform.localRotation, rotationSmoothingValue * Time.deltaTime * snapMulti);
                transform.localRotation = smoothedRotation;
            }
        }
        else
        {
            smoothedPosition = transform.localPosition;
            smoothedRotation = transform.localRotation;
        }
    }
}