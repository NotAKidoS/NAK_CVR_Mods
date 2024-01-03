using System.Runtime.CompilerServices;
using MelonLoader;
using UIExpansionKit.API;

namespace NAK.IKFixes.Integrations;

public static class UIExKitAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        var settings = ExpansionKitApi.GetSettingsCategory(IKFixes.SettingsCategory);
        settings.AddSimpleButton("Reset Settings (Only visually updates bool values, UIExpansionKit bug!)", ResetSettings);
    }

    private static void ResetSettings()
    {
        foreach (MelonPreferences_Entry setting in IKFixes.Category.Entries)
            setting.ResetToDefault();
    }
}