using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using UnityEngine;

namespace NAK.OriginShift.Extensions;

public static class BetterBetterCharacterControllerExtensions
{
    /// <summary>
    /// Offsets the player by the given vector.
    /// This is a simple move operation that does not affect velocity, grounded state, movement parent, ect.
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="offset"></param>
    public static void OffsetBy(this BetterBetterCharacterController controller, Vector3 offset)
    {
        controller.MoveTo(PlayerSetup.Instance.GetPlayerPosition() + offset);
    }
    
    /// <summary>
    /// Moves the player to the target position while keeping velocity, grounded state, movement parent, ect.
    /// Allows moving the player in a way that is meant to be seamless, unlike TeleportTo or SetPosition.
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="targetPos"></param>
    /// <param name="interpolate"></param>
    public static void MoveTo(this BetterBetterCharacterController controller, Vector3 targetPos,
        bool interpolate = false)
    {
        // character controller is not built to account for the player's VR offset 
        Vector3 vector = targetPos - PlayerSetup.Instance.GetPlayerPosition();
        Vector3 vector2 = controller.GetPosition() + vector;
        
        if (!CVRTools.IsWithinMaxBounds(vector2))
        {
            // yeah, ill play your game
            CommonTools.LogAuto(CommonTools.LogLevelType_t.Warning,
                "Attempted to move player further than the maximum allowed bounds.", "",
                "OriginShift/Extensions/BetterBetterCharacterControllerExtensions.cs",
                "MoveTo", 19);
            return;
        }
        
        controller.TeleportPosition(vector2, interpolate); // move player
        controller.SetVelocity(controller.characterMovement.velocity); // keep velocity
        controller.UpdateColliderCenter(vector2, true); // update collider center
        controller.characterMovement.UpdateCurrentPlatform(); // recalculate stored local offset
        
        // invoke event so ik can update
        BetterBetterCharacterController.OnMovementParentMove.Invoke(
            new BetterBetterCharacterController.PlayerMoveOffset(
                PlayerSetup.Instance.GetPlayerPosition(), 
                vector,
                Quaternion.identity));
    }
}