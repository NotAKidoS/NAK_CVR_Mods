using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.ASTExtension.Extensions;

public static class PlayerSetupExtensions
{
    // immediate measurement of the player's avatar height
    public static float GetCurrentAvatarHeight(this PlayerSetup playerSetup)
    {
        Vector3 localScale = playerSetup._avatar.transform.localScale;
        Vector3 initialScale = playerSetup.initialScale;
        float initialHeight = playerSetup._initialAvatarHeight;
        Vector3 scaleDifference = PlayerSetup.DivideVectors(localScale - initialScale, initialScale);
        return initialHeight + initialHeight * scaleDifference.y;
    }
}