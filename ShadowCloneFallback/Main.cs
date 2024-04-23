using ABI_RC.Core.Player.ShadowClone;
using MelonLoader;

namespace NAK.ShadowCloneFallback;

public class ShadowCloneFallback : MelonMod
{
    private const string SettingsCategory = nameof(ShadowCloneFallback);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    private static readonly MelonPreferences_Entry<bool> EntryUseFallbackClones =
        Category.CreateEntry("Use Fallback Clones", true, description: "Toggle ShadowCloneFallback entirely.");
    
    public override void OnInitializeMelon()
    {
        ShadowCloneUtils.s_UseShaderClones = EntryUseFallbackClones.Value;
        EntryUseFallbackClones.OnEntryValueChanged.Subscribe(OnEnabledChanged);
    }
    
    private void OnEnabledChanged(bool _, bool __)
    {
        ShadowCloneUtils.s_UseShaderClones = !EntryUseFallbackClones.Value;
        LoggerInstance.Msg($"ShadowCloneFallback is now {(EntryUseFallbackClones.Value ? "enabled" : "disabled")}.");
    }
}