using System.Runtime.CompilerServices;
using UIExpansionKit.API;

namespace NAK.Melons.Blackout;
public static class UIExpansionKitAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        /**
            ive spent hours debugging this, and no matter what the buttons wont actually call the actions
            from logging shit to straight up closing the game, nothing

            implementing btkuilib support, but gonna leave this shit as a reminder why to not use uiexpansionkit
            also because it **used to work**... a game update broke it and uiexpansionkit hasnt updated since

            what pisses me off more, is that DesktopVRSwitch works, and that was originally copied from Blackout -_-
            https://github.com/NotAKidOnSteam/DesktopVRSwitch/blob/main/DesktopVRSwitch/UIExpansionKitAddon.cs
        **/
        var settings = ExpansionKitApi.GetSettingsCategory(Blackout.SettingsCategory);
        settings.AddSimpleButton("Awake State", AwakeState);
        settings.AddSimpleButton("Drowsy State", DrowsyState);
        settings.AddSimpleButton("Sleep State", SleepingState);
    }
    //UIExpansionKit actions
    internal static void AwakeState() => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Awake);
    internal static void DrowsyState() => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Drowsy);
    internal static void SleepingState() => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Sleeping);
}