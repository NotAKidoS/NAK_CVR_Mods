using MelonLoader;
using ABI_RC.Systems.GameEventSystem;

namespace NAK.MuteSFX;

public class MuteSFX : MelonMod
{
    #region Mod Settings

    internal static MelonLogger.Instance Logger;
    private const string SettingsCategory = nameof(MuteSFX);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle MuteSFX entirely.");

    #endregion

    #region Melon Initialization

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        CVRGameEventSystem.Microphone.OnMute.AddListener(() => { OnMicrophoneStatusChanged(false); });
        CVRGameEventSystem.Microphone.OnUnmute.AddListener(() => { OnMicrophoneStatusChanged(true); });

        AudioModuleManager.SetupDefaultAudioClips();
    }

    #endregion

    #region Patched Methods

    private static void OnMicrophoneStatusChanged(bool active)
    {
        if (EntryEnabled.Value)
            AudioModuleManager.PlayAudioModule(active ? AudioModuleManager.sfx_unmute : AudioModuleManager.sfx_mute);
    }
    
    #endregion
}