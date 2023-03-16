using ABI_RC.Core.Player;
using MelonLoader;
using System.Collections;

namespace NAK.Melons.FuckMetrics;

public class FuckMetricsMod : MelonMod
{
    public static MelonLogger.Instance Logger;
    public const string SettingsCategory = "FuckMetrics";
    public static readonly MelonPreferences_Category CategoryFuckMetrics = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryDisableCohtmlViewOnIdle =
        CategoryFuckMetrics.CreateEntry("Disable CohtmlView On Idle", false, description: "Disables CohtmlView on the menus when idle. Takes up to 6 seconds after menu exit. This can give a huge performance boost.");

    public static readonly MelonPreferences_Entry<SettingState> EntryDisableMetrics =
        CategoryFuckMetrics.CreateEntry("Menu Metrics", SettingState.MenuOnly, description: "Disables menu metrics (FPS & Ping). Updates once on menu open if disabled.");

    public static readonly MelonPreferences_Entry<SettingState> EntryDisableCoreUpdates =
        CategoryFuckMetrics.CreateEntry("Menu Core Updates", SettingState.MenuOnly, description: "Disables menu core updates (Gamerule Icons & Debug Status). Updates once on menu open if disabled.");

    public static readonly MelonPreferences_Entry<float> EntryMetricsUpdateRate =
        CategoryFuckMetrics.CreateEntry("Metrics Update Rate", 1f, description: "Sets the update rate for the menu metrics. Default is 0.5f. Recommended to be 1f or higher.");

    public static readonly MelonPreferences_Entry<float> EntryCoreUpdateRate =
        CategoryFuckMetrics.CreateEntry("Core Update Rate", 2f, description: "Sets the update rate for the menu core updates. Default is 0.1f. Recommended to be 2f or higher.");
    public enum SettingState
    {
        Always,
        MenuOnly,
        Disabled
    }

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EntryDisableMetrics.OnEntryValueChangedUntyped.Subscribe(OnDisableMetrics);
        EntryDisableCoreUpdates.OnEntryValueChangedUntyped.Subscribe(OnDisableCoreUpdates);
        EntryMetricsUpdateRate.OnEntryValueChangedUntyped.Subscribe(OnChangeMetricsUpdateRate);
        EntryCoreUpdateRate.OnEntryValueChangedUntyped.Subscribe(OnChangeCoreUpdateRate);
        ApplyPatches(typeof(HarmonyPatches.CVR_MenuManagerPatches));
        ApplyPatches(typeof(HarmonyPatches.ViewManagerPatches));
        ApplyPatches(typeof(HarmonyPatches.CohtmlViewPatches));
        MelonCoroutines.Start(WaitForLocalPlayer());
    }

    IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;
        UpdateSettings();
    }

    private void OnDisableMetrics(object arg1, object arg2)
    {
        FuckMetrics.ToggleMetrics(EntryDisableMetrics.Value == SettingState.Always);
    }

    private void OnDisableCoreUpdates(object arg1, object arg2)
    {
        FuckMetrics.ToggleCoreUpdates(EntryDisableCoreUpdates.Value == SettingState.Always);
    }

    private void UpdateSettings()
    {
        FuckMetrics.ToggleMetrics(EntryDisableMetrics.Value == SettingState.Always);
        FuckMetrics.ToggleCoreUpdates(EntryDisableCoreUpdates.Value == SettingState.Always);
    }

    private void OnChangeMetricsUpdateRate(object arg1, object arg2)
    {
        if (EntryDisableMetrics.Value != SettingState.Disabled)
        {
            FuckMetrics.ToggleMetrics(false);
            FuckMetrics.ToggleMetrics(true);
        }
    }

    private void OnChangeCoreUpdateRate(object arg1, object arg2)
    {
        if (EntryDisableCoreUpdates.Value != SettingState.Disabled)
        {
            FuckMetrics.ToggleCoreUpdates(false);
            FuckMetrics.ToggleCoreUpdates(true);
        }
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            Logger.Msg($"Failed while patching {type.Name}!");
            Logger.Error(e);
        }
    }
}