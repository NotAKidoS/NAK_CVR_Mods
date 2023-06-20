using UnityEngine;

namespace NAK.DesktopVRSwitch;

internal class TryCatchHell
{
    public delegate void TryAction(bool intoVR);

    public static void TryExecute(TryAction action, bool intoVR)
    {
        try
        {
            action(intoVR);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing action: {ex.Message}");
        }
    }

    internal static void CloseCohtmlMenus(bool intoVR)
    {


    }

    internal static void RepositionCohtmlHud(bool intoVR)
    {

    }

    internal static void UpdateHudOperations(bool intoVR)
    {

    }

    internal static void DisableMirrorCanvas(bool intoVR)
    {

    }

    internal static void SwitchActiveCameraRigs(bool intoVR)
    {


    }

    internal static void PauseInputInteractions(bool intoVR)
    {

    }

    internal static void ReloadLocalAvatar(bool intoVR)
    {

    }

    internal static void UpdateGestureReconizerCam(bool intoVR)
    {

    }

    internal static void UpdateMenuCoreData(bool intoVR)
    {

    }
}
