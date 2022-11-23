using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;

namespace Blackout;

public class Blackout : MelonMod
{
    internal static bool inVR;
    internal const string SettingsCategory = "Blackout";

    private static MelonPreferences_Category m_categoryBlackout;
    private static MelonPreferences_Entry<bool> m_entryEnabled, m_entryHudMessages, m_entryDropFPSOnSleep;
    private static MelonPreferences_Entry<float>
        m_entryDrowsyThreshold, m_entryAwakeThreshold,
        m_entryDrowsyModeTimer, m_entrySleepModeTimer,
        m_entryDrowsyDimStrength;

    public override void OnInitializeMelon()
    {
        m_categoryBlackout = MelonPreferences.CreateCategory(nameof(Blackout));
        m_entryEnabled = m_categoryBlackout.CreateEntry<bool>("Automatic State Change", true, description: "Dim screen when there is no movement for a while.");
        m_entryHudMessages = m_categoryBlackout.CreateEntry<bool>("Hud Messages", false, description: "Notify on state change.");
        m_entryDropFPSOnSleep = m_categoryBlackout.CreateEntry<bool>("Lower FPS While Sleep", false, description: "Lowers FPS to 5 while in Sleep State.");
        m_entryDrowsyThreshold = m_categoryBlackout.CreateEntry<float>("Drowsy Threshold", 1f, description: "Degrees of movement to return partial vision.");
        m_entryAwakeThreshold = m_categoryBlackout.CreateEntry<float>("Awake Threshold", 12f, description: "Degrees of movement to return full vision.");
        m_entryDrowsyModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Drowsy Time (Minutes)", 3f, description: "How many minutes without movement until enter drowsy mode.");
        m_entrySleepModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Sleep Time (Seconds)", 10f, description: "How many seconds without movement until enter sleep mode.");
        m_entryDrowsyDimStrength = m_categoryBlackout.CreateEntry<float>("Drowsy Dim Strength", 0.5f, description: "How strong of a dimming effect should drowsy mode have.");
        m_categoryBlackout.SaveToFile(false);

        foreach (var setting in m_categoryBlackout.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        //UIExpansionKit addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "UI Expansion Kit"))
        {
            MelonLogger.Msg("Initializing UIExpansionKit support.");
            UiExtensionsAddon.Init();
        }

        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        //load blackout_controller.asset
        AssetsHandler.Load();

        while (PlayerSetup.Instance == null)
            yield return null;

        inVR = MetaPort.Instance.isUsingVr;
        PlayerSetup.Instance.gameObject.AddComponent<BlackoutController>();

        //update BlackoutController settings after it initializes
        while (BlackoutController.Instance == null)
            yield return null;

        UpdateAllSettings();
    }

    private void OnEnabled()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.enabled = m_entryEnabled.Value;
    }

    private void UpdateAllSettings()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.enabled = m_entryEnabled.Value;
        BlackoutController.Instance.HudMessages = m_entryHudMessages.Value;
        BlackoutController.Instance.DropFPSOnSleep = m_entryDropFPSOnSleep.Value;
        BlackoutController.Instance.drowsyThreshold = m_entryDrowsyThreshold.Value;
        BlackoutController.Instance.wakeThreshold = m_entryAwakeThreshold.Value;
        BlackoutController.Instance.DrowsyModeTimer = m_entryDrowsyModeTimer.Value;
        BlackoutController.Instance.SleepModeTimer = m_entrySleepModeTimer.Value;
        BlackoutController.Instance.DrowsyDimStrength = m_entryDrowsyDimStrength.Value;
    }

    private void OnUpdateEnabled(object arg1, object arg2) => OnEnabled();
    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();

    //UIExpansionKit actions
    internal static void AwakeState() => BlackoutController.Instance.ChangeBlackoutState(BlackoutController.BlackoutState.Awake);
    internal static void DrowsyState() => BlackoutController.Instance.ChangeBlackoutState(BlackoutController.BlackoutState.Drowsy);
    internal static void SleepingState() => BlackoutController.Instance.ChangeBlackoutState(BlackoutController.BlackoutState.Sleeping);
}