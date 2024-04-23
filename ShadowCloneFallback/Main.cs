using ABI_RC.Core.Player.ShadowClone;
using MelonLoader;

namespace NAK.ShadowCloneFallback;

public class ShadowCloneFallback : MelonMod
{
    private const string SettingsCategory = nameof(ShadowCloneFallback);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle ShadowCloneFallback entirely.");
    
    public override void OnInitializeMelon()
    {
        ShadowCloneUtils.s_UseShaderClones = EntryEnabled.Value;
        EntryEnabled.OnEntryValueChanged.Subscribe(OnEnabledChanged);
    }
    
    private void OnEnabledChanged(bool _, bool __)
    {
        ShadowCloneUtils.s_UseShaderClones = EntryEnabled.Value;
        LoggerInstance.Msg($"ShadowCloneUtils.s_UseShaderClones is now {(EntryEnabled.Value ? "enabled" : "disabled")}.");
    }
}