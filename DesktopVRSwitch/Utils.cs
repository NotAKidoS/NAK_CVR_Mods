using ABI_RC.Core.EventSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using UnityEngine;

namespace NAK.DesktopVRSwitch;

internal static class Utils
{
    internal static GameObject GetPlayerCameraObject(bool intoVR)
    {
        if (intoVR)
        {
            return PlayerSetup.Instance.vrCamera;
        }
        return PlayerSetup.Instance.desktopCamera;
    }

    //stole from kafe :>
    internal static Vector3 GetPlayerRootPosition()
    {
        return MovementSystem.Instance.rotationPivot.position with
        {
            y = MovementSystem.Instance.transform.position.y
        };
    }

    internal static void ReloadLocalAvatar()
    {
        DesktopVRSwitch.Logger.Msg("Attempting to reload current local avatar from GUID.");
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);
    }
}
