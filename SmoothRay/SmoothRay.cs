/**
    MIT License

    Copyright (c) 2021 Kinsi, NotAKidOnSteam

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
**/

using ABI_RC.Core.InteractionSystem;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

namespace NAK.SmoothRay;

public class SmoothRayer : MonoBehaviour
{
    public ControllerRay ray;

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
            newPosesAction.enabled = true;
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