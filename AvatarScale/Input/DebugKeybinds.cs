using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.InputHandling;

public static class DebugKeybinds
{
    public static void DoDebugInput()
    {
        if (AvatarScaleManager.Instance == null)
            return;

        float currentHeight;
        const float step = 0.1f;

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            currentHeight = AvatarScaleManager.Instance.GetHeight() + step;
            AvatarScaleManager.Instance.SetHeight(currentHeight);
            
            AvatarScaleMod.Logger.Msg($"Setting height: {currentHeight}");
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            currentHeight = AvatarScaleManager.Instance.GetHeight() - step;
            AvatarScaleManager.Instance.SetHeight(currentHeight);
            
            AvatarScaleMod.Logger.Msg($"Setting height: {currentHeight}");
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            AvatarScaleManager.Instance.ResetHeight();
            
            AvatarScaleMod.Logger.Msg($"Resetting height.");
        }
    }
}