using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;

namespace NAK.Blackout;

public class Blackout : MelonMod
{
    internal static bool inVR;
    internal const string SettingsCategory = "Blackout";

    internal static MelonPreferences_Category m_categoryBlackout;
    internal static MelonPreferences_Entry<bool>
        m_entryEnabled,
        m_entryAutoSleepState,
        m_entryHudMessages,
        m_entryDropFPSOnSleep,
        m_entryDrowsyVelocityMultiplier;
    internal static MelonPreferences_Entry<float>
        m_entryDrowsyThreshold, m_entryAwakeThreshold,
        m_entryDrowsyModeTimer, m_entrySleepModeTimer,
        m_entryDrowsyDimStrength;

    public override void OnInitializeMelon()
    {
        m_categoryBlackout = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryEnabled = m_categoryBlackout.CreateEntry<bool>("Automatic State Change", true, description: "Should the screen automatically dim if head is still for enough time?");
        m_entryEnabled.OnEntryValueChangedUntyped.Subscribe(OnUpdateEnabled);
        m_entryHudMessages = m_categoryBlackout.CreateEntry<bool>("Hud Messages", true, description: "Notify on state change.");
        m_entryDropFPSOnSleep = m_categoryBlackout.CreateEntry<bool>("Limit FPS While Sleep", false, description: "Limits FPS to 5 while in Sleep State. This only works in Desktop, as SteamVR/HMD handles VR FPS.");
        m_entryDrowsyVelocityMultiplier = m_categoryBlackout.CreateEntry<bool>("Drowsy Velocity Multiplier", true, description: "Should head velocity act as a multiplier to Drowsy Dim Strength?");
        m_entryAutoSleepState = m_categoryBlackout.CreateEntry<bool>("Auto Sleep State", true, description: "Should the sleep state be used during Automatic State Change?");
        m_entryDrowsyThreshold = m_categoryBlackout.CreateEntry<float>("Drowsy Threshold", 2f, description: "Velocity to return partial vision.");
        m_entryAwakeThreshold = m_categoryBlackout.CreateEntry<float>("Awake Threshold", 4f, description: "Velocity to return full vision.");
        m_entryDrowsyModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Drowsy Time (Minutes)", 3f, description: "How many minutes without movement until enter drowsy mode.");
        m_entrySleepModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Sleep Time (Seconds)", 10f, description: "How many seconds without movement until enter sleep mode.");
        m_entryDrowsyDimStrength = m_categoryBlackout.CreateEntry<float>("Drowsy Dim Strength", 0.6f, description: "How strong of a dimming effect should drowsy mode have.");

        foreach (var setting in m_categoryBlackout.Entries)
        {
            if (!setting.OnEntryValueChangedUntyped.GetSubscribers().Any())
                setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        //UIExpansionKit addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "UI Expansion Kit"))
        {
            MelonLogger.Msg("Initializing UIExpansionKit support.");
            UIExpansionKitAddon.Init();
        }

        //BTKUILib addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            MelonLogger.Msg("Initializing BTKUILib support.");
            BTKUIAddon.Init();
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
        OnEnabled();
    }

    private void OnEnabled()
    {
        if (!BlackoutController.Instance) return;
        if (m_entryEnabled.Value)
        {
            BlackoutController.Instance.OnEnable();
        }
        else
        {
            BlackoutController.Instance.OnDisable();
        }
        BlackoutController.Instance.AutomaticStateChange = m_entryEnabled.Value;
    }

    private void UpdateAllSettings()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.HudMessages = m_entryHudMessages.Value;
        BlackoutController.Instance.AutoSleepState = m_entryAutoSleepState.Value;
        BlackoutController.Instance.DropFPSOnSleep = m_entryDropFPSOnSleep.Value;
        BlackoutController.Instance.drowsyThreshold = m_entryDrowsyThreshold.Value;
        BlackoutController.Instance.wakeThreshold = m_entryAwakeThreshold.Value;
        BlackoutController.Instance.DrowsyModeTimer = m_entryDrowsyModeTimer.Value;
        BlackoutController.Instance.SleepModeTimer = m_entrySleepModeTimer.Value;
        BlackoutController.Instance.DrowsyDimStrength = m_entryDrowsyDimStrength.Value;
        BlackoutController.Instance.DrowsyVelocityMultiplier = m_entryDrowsyVelocityMultiplier.Value;
    }

    private void OnUpdateEnabled(object arg1, object arg2) => OnEnabled();
    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
}