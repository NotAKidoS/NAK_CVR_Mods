using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.GestureReconizer;

public static class ScaleReconizer
{
    public static bool Enabled = true;

    // Require triggers to be down while doing fist - Exteratta
    public static bool RequireTriggers = true;

    // Initial values when scale gesture is started
    private static float _initialModifier;
    private static float _initialTargetHeight;
    
    public static void Initialize()
    {
        // This requires arms far outward- pull inward with fist and triggers.
        // Release triggers while still holding fist to readjust.
        
        CVRGesture gesture = new CVRGesture
        {
            name = "avatarScaleIn",
            type = CVRGesture.GestureType.Hold
        };
        gesture.steps.Add(new CVRGestureStep
        {
            firstGesture = CVRGestureStep.Gesture.Fist,
            secondGesture = CVRGestureStep.Gesture.Fist,
            startDistance = 1f,
            endDistance = 0.25f,
            direction = CVRGestureStep.GestureDirection.MovingIn,
            needsToBeInView = true,
        });
        gesture.onStart.AddListener(OnScaleStart);
        gesture.onStay.AddListener(OnScaleStay);
        gesture.onEnd.AddListener(OnScaleEnd);
        CVRGestureRecognizer.Instance.gestures.Add(gesture);

        gesture = new CVRGesture
        {
            name = "avatarScaleOut",
            type = CVRGesture.GestureType.Hold
        };
        gesture.steps.Add(new CVRGestureStep
        {
            firstGesture = CVRGestureStep.Gesture.Fist,
            secondGesture = CVRGestureStep.Gesture.Fist,
            startDistance = 0.25f,
            endDistance = 1f,
            direction = CVRGestureStep.GestureDirection.MovingOut,
            needsToBeInView = true,
        });
        gesture.onStart.AddListener(OnScaleStart);
        gesture.onStay.AddListener(OnScaleStay);
        gesture.onEnd.AddListener(OnScaleEnd);
        CVRGestureRecognizer.Instance.gestures.Add(gesture);
    }

    private static void OnScaleStart(float modifier, Transform transform1, Transform transform2)
    {
        if (!Enabled)
            return;
        
        // Store initial modifier so we can get difference later
        _initialModifier = Mathf.Max(modifier, 0.01f); // no zero
        _initialTargetHeight = AvatarScaleManager.Instance.GetHeight();
    }

    private static void OnScaleStay(float modifier, Transform transform1, Transform transform2)
    {
        if (!Enabled)
            return;

        modifier = Mathf.Max(modifier, 0.01f); // no zero

        // Allow user to release triggers to reset "world grip"
        if (RequireTriggers && !AreBothTriggersDown())
        {
            _initialModifier = modifier;
            _initialTargetHeight = AvatarScaleManager.Instance.GetHeight();
            return;
        }
        
        // Invert so the gesture is more of a world squish instead of happy hug
        float modifierRatio = 1f / (modifier / _initialModifier);

        // Determine the adjustment factor for the height, this will be >1 if scaling up, <1 if scaling down.
        float heightAdjustmentFactor = (modifierRatio > 1) ? 1 + (modifierRatio - 1) : 1 - (1 - modifierRatio);

        // Apply the adjustment to the target height
        AvatarScaleManager.Instance.SetHeight(_initialTargetHeight * heightAdjustmentFactor);
    }

    private static void OnScaleEnd(float modifier, Transform transform1, Transform transform2)
    {
        // Unused, needed for mod network?
    }
    
    private static bool AreBothTriggersDown()
    {
        // Maybe it should be one trigger? Imagine XSOverlay scaling but for player.
        return CVRInputManager.Instance.interactLeftValue > 0.75f && CVRInputManager.Instance.interactRightValue > 0.75f;
    }
}