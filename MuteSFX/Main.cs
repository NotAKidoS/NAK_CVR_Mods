using ABI_RC.Core.Base;
using MelonLoader;
using System.Reflection;
using HarmonyLib;

namespace NAK.MuteSFX;

public class MuteSFX : MelonMod
{
    #region Mod Settings

    internal static MelonLogger.Instance Logger;
    internal const string SettingsCategory = nameof(MuteSFX);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle MuteSFX entirely.");

    #endregion

    #region Melon Initialization

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        HarmonyInstance.Patch(
            typeof(AudioManagement).GetMethod(nameof(AudioManagement.SetMicrophoneActive)),
            postfix: new HarmonyMethod(typeof(MuteSFX).GetMethod(nameof(OnSetMicrophoneActive_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        AudioModuleManager.SetupDefaultAudioClips();
    }

    #endregion

    #region Patched Methods

    private static void OnSetMicrophoneActive_Postfix(bool active)
    {
        try
        {
            if (EntryEnabled.Value)
                AudioModuleManager.PlayAudioModule(active ? AudioModuleManager.sfx_unmute : AudioModuleManager.sfx_mute);
        }
        catch (Exception e)
        {
            Logger.Error($"Error during the patched method {nameof(OnSetMicrophoneActive_Postfix)}");
            Logger.Error(e);
        }
    }
    
    #endregion
}