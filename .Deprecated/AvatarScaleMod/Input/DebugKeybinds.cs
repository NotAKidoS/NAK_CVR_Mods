using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.InputHandling;

internal static class DebugKeybinds
{
    private const float Step = 0.1f;

    internal static void DoDebugInput()
    {
        if (AvatarScaleManager.Instance == null)
            return;

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AdjustHeight(Step);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            AdjustHeight(-Step);
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetHeight();
        }
    }

    private static void AdjustHeight(float adjustment)
    {
        float currentHeight = AvatarScaleManager.Instance.GetHeight() + adjustment;
        currentHeight = Mathf.Max(0f, currentHeight);
        AvatarScaleManager.Instance.SetTargetHeight(currentHeight);

        AvatarScaleMod.Logger.Msg($"[Debug] Setting height: {currentHeight}");
    }   

    private static void ResetHeight()
    {
        AvatarScaleManager.Instance.Setting_UniversalScaling = false;
        AvatarScaleMod.Logger.Msg("[Debug] Resetting height.");
    }
}