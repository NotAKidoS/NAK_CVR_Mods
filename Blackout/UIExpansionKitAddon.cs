using System.Runtime.CompilerServices;
using UIExpansionKit.API;

namespace Blackout;
public static class UiExtensionsAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        var settings = ExpansionKitApi.GetSettingsCategory(Blackout.SettingsCategory);
        settings.AddSimpleButton("Awake State", Blackout.AwakeState);
        settings.AddSimpleButton("Drowsy State", Blackout.DrowsyState);
        settings.AddSimpleButton("Sleep State", Blackout.SleepingState);
    }
}