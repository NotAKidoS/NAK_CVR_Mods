using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;

namespace NAK.Blackout;

public class Blackout : MelonMod
{
    internal static bool inVR;
    internal static MelonLogger.Instance Logger;
    internal const string SettingsCategory = nameof(Blackout);

    public static readonly MelonPreferences_Category CategoryBlackout = 
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled = 
        CategoryBlackout.CreateEntry("Automatic State Change", true, "Should the screen automatically dim if head is still for enough time?");

    public static readonly MelonPreferences_Entry<bool> EntryHudMessages = 
        CategoryBlackout.CreateEntry("Hud Messages", true, "Notify on state change.");

    public static readonly MelonPreferences_Entry<bool> EntryDropFPSOnSleep = 
        CategoryBlackout.CreateEntry("Limit FPS While Sleep", false, "Limits FPS to 5 while in Sleep State. This only works in Desktop, as SteamVR/HMD handles VR FPS.");

    public static readonly MelonPreferences_Entry<bool> EntryDrowsyVelocityMultiplier = 
        CategoryBlackout.CreateEntry("Drowsy Velocity Multiplier", true, "Should head velocity act as a multiplier to Drowsy Dim Strength?");

    public static readonly MelonPreferences_Entry<bool> EntryAutoSleepState = 
        CategoryBlackout.CreateEntry("Auto Sleep State", true, "Should the sleep state be used during Automatic State Change?");

    public static readonly MelonPreferences_Entry<float> EntryDrowsyThreshold = 
        CategoryBlackout.CreateEntry("Drowsy Threshold", 2f, "Velocity to return partial vision.");

    public static readonly MelonPreferences_Entry<float> EntryAwakeThreshold = 
        CategoryBlackout.CreateEntry("Awake Threshold", 4f, "Velocity to return full vision.");

    public static readonly MelonPreferences_Entry<float> EntryDrowsyModeTimer = 
        CategoryBlackout.CreateEntry("Enter Drowsy Time (Minutes)", 3f, "How many minutes without movement until enter drowsy mode.");

    public static readonly MelonPreferences_Entry<float> EntrySleepModeTimer = 
        CategoryBlackout.CreateEntry("Enter Sleep Time (Seconds)", 10f, "How many seconds without movement until enter sleep mode.");

    public static readonly MelonPreferences_Entry<float> EntryDrowsyDimStrength = 
        CategoryBlackout.CreateEntry("Drowsy Dim Strength", 0.6f, "How strong of a dimming effect should drowsy mode have.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EntryEnabled.OnEntryValueChangedUntyped.Subscribe(OnUpdateEnabled);
        foreach (var entry in CategoryBlackout.Entries)
        {
            if (entry != EntryEnabled && !entry.OnEntryValueChangedUntyped.GetSubscribers().Any())
            {
                entry.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
            }
        }

        //UIExpansionKit addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "UI Expansion Kit"))
        {
            Logger.Msg("Initializing UIExpansionKit support.");
            UIExpansionKitAddon.Init();
        }

        //BTKUILib addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            Logger.Msg("Initializing BTKUILib support.");
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
        if (EntryEnabled.Value)
        {
            BlackoutController.Instance.OnEnable();
        }
        else
        {
            BlackoutController.Instance.OnDisable();
        }
        BlackoutController.Instance.AutomaticStateChange = EntryEnabled.Value;
    }

    private void UpdateAllSettings()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.HudMessages = EntryHudMessages.Value;
        BlackoutController.Instance.AutoSleepState = EntryAutoSleepState.Value;
        BlackoutController.Instance.DropFPSOnSleep = EntryDropFPSOnSleep.Value;
        BlackoutController.Instance.drowsyThreshold = EntryDrowsyThreshold.Value;
        BlackoutController.Instance.wakeThreshold = EntryAwakeThreshold.Value;
        BlackoutController.Instance.DrowsyModeTimer = EntryDrowsyModeTimer.Value;
        BlackoutController.Instance.SleepModeTimer = EntrySleepModeTimer.Value;
        BlackoutController.Instance.DrowsyDimStrength = EntryDrowsyDimStrength.Value;
        BlackoutController.Instance.DrowsyVelocityMultiplier = EntryDrowsyVelocityMultiplier.Value;
    }

    private void OnUpdateEnabled(object arg1, object arg2) => OnEnabled();
    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
}