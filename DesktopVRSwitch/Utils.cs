using ABI_RC.Core.EventSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
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

    internal static void ReloadLocalAvatar()
    {
        DesktopVRSwitch.Logger.Msg("Attempting to reload current local avatar from GUID.");
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);
    }
}
