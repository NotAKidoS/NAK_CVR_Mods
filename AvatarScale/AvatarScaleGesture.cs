using UnityEngine;
using ABI_RC.Core.Savior;

namespace NAK.AvatarScaleMod;

public static class AvatarScaleGesture
{
    public static bool GestureEnabled;
    public static bool RequireTriggers = true;
    public static float InitialModifier = 1f;
    public static float InitialTargetHeight = 1.8f;

    public static void OnScaleStart(float modifier, Transform transform1, Transform transform2)
    {
        // AvatarScaleMod.Logger.Msg("OnScaleStart!");
        if (!GestureEnabled)
            return;

        // you can start the scale, but cant interact with it without holding triggers

        if (AvatarScaleManager.LocalAvatar != null)
        {
            // store initial modifier
            InitialModifier = modifier;
            InitialTargetHeight = AvatarScaleManager.LocalAvatar.TargetHeight;
        }
    }

    public static void OnScaleStay(float modifier, Transform transform1, Transform transform2)
    {
        // AvatarScaleMod.Logger.Msg("OnScaleStay!");
        if (!GestureEnabled)
            return;

        if (RequireTriggers && !IsBothTriggersDown())
            return;

        if (AvatarScaleManager.LocalAvatar != null)
        {
            float modifierRatio = modifier / InitialModifier;

            // Determine the adjustment factor for the height, this will be >1 if scaling up, <1 if scaling down.
            float heightAdjustmentFactor = (modifierRatio > 1) ? 1 + (modifierRatio - 1) : 1 - (1 - modifierRatio);

            // Apply the adjustment to the target height
            AvatarScaleManager.LocalAvatar.SetTargetHeight(InitialTargetHeight * heightAdjustmentFactor);
        }
    }

    public static void OnScaleEnd(float modifier, Transform transform1, Transform transform2)
    {
        // AvatarScaleMod.Logger.Msg("OnScaleEnd!");
    }

    public static bool IsBothTriggersDown()
    {
        return CVRInputManager.Instance.interactLeftValue > 0.75f && CVRInputManager.Instance.interactRightValue > 0.75f;
    }
}
