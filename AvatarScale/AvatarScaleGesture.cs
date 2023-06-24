using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.AvatarScaleMod;

public static class AvatarScaleGesture
{
    // Toggle for scale gesture
    public static bool GestureEnabled;

    // Require triggers to be down while doing fist? - Exteratta 
    public static bool RequireTriggers = true;

    // Initial values when scale gesture is started
    public static float InitialModifier;
    public static float InitialTargetHeight;

    public static void OnScaleStart(float modifier, Transform transform1, Transform transform2)
    {
        if (!GestureEnabled)
            return;

        if (AvatarScaleManager.LocalAvatar != null)
        {
            // Store initial modifier so we can get difference later
            InitialModifier = modifier;
            InitialTargetHeight = AvatarScaleManager.LocalAvatar.TargetHeight;
        }
    }

    public static void OnScaleStay(float modifier, Transform transform1, Transform transform2)
    {
        if (!GestureEnabled)
            return;

        // Allow user to release triggers to reset "world grip"
        if (RequireTriggers && !AreBothTriggersDown())
        {
            InitialModifier = modifier;
            InitialTargetHeight = AvatarScaleManager.LocalAvatar.TargetHeight;
            return;
        }

        if (AvatarScaleManager.LocalAvatar != null)
        {
            // Invert so the gesture is more of a world squish instead of happy hug
            float modifierRatio = 1f / (modifier / InitialModifier);

            // Determine the adjustment factor for the height, this will be >1 if scaling up, <1 if scaling down.
            float heightAdjustmentFactor = (modifierRatio > 1) ? 1 + (modifierRatio - 1) : 1 - (1 - modifierRatio);

            // Apply the adjustment to the target height
            AvatarScaleManager.LocalAvatar.SetTargetHeight(InitialTargetHeight * heightAdjustmentFactor);
        }
    }
    
    public static void OnScaleEnd(float modifier, Transform transform1, Transform transform2)
    {
        // Unused, needed for mod network?
    }
    
    // Maybe it should be one trigger? Imagine XSOverlay scaling but for player.
    public static bool AreBothTriggersDown()
    {
        return CVRInputManager.Instance.interactLeftValue > 0.75f && CVRInputManager.Instance.interactRightValue > 0.75f;
    }
}