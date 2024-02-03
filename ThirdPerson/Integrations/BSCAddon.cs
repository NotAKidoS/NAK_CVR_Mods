using NAK.BetterShadowClone;

namespace NAK.ThirdPerson.Integrations;

public static class BSCAddon
{
    public static void Initialize()
    {
        ShadowCloneMod.wantsToHideHead += CameraLogic.ShouldNotHideHead_ThirdPerson;
    }
}