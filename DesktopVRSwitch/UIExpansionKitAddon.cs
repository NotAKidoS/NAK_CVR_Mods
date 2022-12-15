using System.Runtime.CompilerServices;
using UIExpansionKit.API;

namespace DesktopVRSwitch;
public static class UiExtensionsAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        var settings = ExpansionKitApi.GetSettingsCategory(DesktopVRSwitch.SettingsCategory);
        settings.AddSimpleButton("Switch VRMode", SwitchModeButton);
    }
    internal static void SwitchModeButton() => DesktopVRSwitchHelper.Instance.SwitchMode(true);
}