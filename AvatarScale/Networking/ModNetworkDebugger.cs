using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Networking;

public static class ModNetworkDebugger
{
    public static void DoDebugInput()
    {
        // if (NetworkManager.Instance == null || NetworkManager.Instance.GameNetwork.ConnectionState != ConnectionState.Connected) 
        // {
        //     MelonLogger.Warning("Attempted to send a game network message without being connected to an online instance...");
        //     return;
        // }

        if (AvatarScaleManager.Instance == null)
            return;

        float currentHeight = AvatarScaleManager.Instance.GetHeight();
        const float step = 0.1f;

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            currentHeight += step;
            AvatarScaleMod.Logger.Msg($"Networking height: {currentHeight}");
            ModNetwork.SendNetworkHeight(currentHeight);
            AvatarScaleManager.Instance.SetHeight(currentHeight);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            currentHeight -= step;
            AvatarScaleMod.Logger.Msg($"Networking height: {currentHeight}");
            ModNetwork.SendNetworkHeight(currentHeight);
            AvatarScaleManager.Instance.SetHeight(currentHeight);
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            AvatarScaleManager.Instance.ResetHeight();
            currentHeight = AvatarScaleManager.Instance.GetHeight();

            AvatarScaleMod.Logger.Msg($"Networking height: {currentHeight}");
            ModNetwork.SendNetworkHeight(currentHeight);
        }
    }
}