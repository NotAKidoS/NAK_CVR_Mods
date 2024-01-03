
namespace NAK.DesktopVRIK.Integrations;

public static class AMTAddon
{
    #region Variables

    public static bool integration_AMT = false;

    #endregion

    #region Initialization

    public static void Initialize()
    {
        integration_AMT = true;
        DesktopVRIK.Logger.Msg("AvatarMotionTweaker was found. Relying on it to handle VRIK locomotion.");
    }

    #endregion
}