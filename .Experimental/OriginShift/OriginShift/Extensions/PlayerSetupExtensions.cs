using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.OriginShift.Extensions;

public static class PlayerSetupExtensions
{
    /// <summary>
    /// Utility method to offset the player's currently stored avatar movement data.
    /// Needs to be called, otherwise outbound net ik will be one-frame behind on teleport events.
    /// </summary>
    /// <param name="playerSetup"></param>
    /// <param name="offset"></param>
    public static void OffsetAvatarMovementData(this PlayerSetup playerSetup, Vector3 offset)
    {
        playerSetup._playerAvatarMovementData.RootPosition += offset;
        playerSetup._playerAvatarMovementData.BodyPosition += offset; // why in world space -_-
    }
}