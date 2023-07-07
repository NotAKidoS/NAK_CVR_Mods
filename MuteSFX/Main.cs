using ABI_RC.Core.Base;
using MelonLoader;
using System.Reflection;

namespace NAK.MuteSFX;

public class MuteSFX : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal const string SettingsCategory = nameof(MuteSFX);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle MuteSFX entirely.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        HarmonyInstance.Patch(
            typeof(Audio).GetMethod(nameof(Audio.SetMicrophoneActive)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(MuteSFX).GetMethod(nameof(OnSetMicrophoneActive_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        AudioModuleManager.SetupDefaultAudioClips();
    }

    private static void OnSetMicrophoneActive_Postfix(bool muted)
    {
        if (EntryEnabled.Value)
            AudioModuleManager.PlayAudioModule(muted ? AudioModuleManager.sfx_mute : AudioModuleManager.sfx_unmute);
    }
}