using ABI_RC.Core;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.ASTExtension.Extensions;

public static class PlayerSetupExtensions
{
    // immediate measurement of the player's avatar height
    public static float GetCurrentAvatarHeight(this PlayerSetup playerSetup)
    {
        if (!playerSetup.IsAvatarLoaded)
        {
            ASTExtensionMod.Logger.Error("GetCurrentAvatarHeight: Avatar is null");
            return 0f;
        }
        
        Vector3 localScale = playerSetup.AvatarTransform.localScale;
        Vector3 initialScale = playerSetup.initialScale;
        float initialHeight = playerSetup._initialAvatarHeight;
        Vector3 scaleDifference = CVRTools.DivideVectors(localScale - initialScale, initialScale);
        return initialHeight + initialHeight * scaleDifference.y;
    }
}