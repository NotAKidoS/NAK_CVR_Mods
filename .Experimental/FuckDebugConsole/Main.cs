using MelonLoader;
using UnityEngine;

namespace NAK.FuckDebugConsole;

public class FuckDebugConsoleMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckDebugConsole));

    internal static readonly MelonPreferences_Entry<bool> EntryEnableDebugConsole =
        Category.CreateEntry("enable_debug_console", false, 
            "Enable Debug Console", description: "Whether to use the debug console at all.");
    
    public override void OnInitializeMelon()
    {
        Debug.developerConsoleEnabled = EntryEnableDebugConsole.Value;
        EntryEnableDebugConsole.OnEntryValueChanged.Subscribe((oldValue, newValue) =>
        {
            Debug.developerConsoleEnabled = newValue;
        });
    }
}