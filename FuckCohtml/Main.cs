using MelonLoader;

namespace NAK.Melons.FuckCohtml;

public class FuckCohtmlMod : MelonMod
{
    public static MelonLogger.Instance Logger;

    public const string SettingsCategory = "FuckCohtml";
    public static readonly MelonPreferences_Category CategoryFuckCohtml = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryDisableMetrics =
        CategoryFuckCohtml.CreateEntry("Disable Metrics", true, description: "Disables menu metrics (FPS & Ping). Updates once on menu open if disabled.");

    public static readonly MelonPreferences_Entry<bool> EntryDisableCoreUpdates =
        CategoryFuckCohtml.CreateEntry("Disable Core Updates", true, description: "Disables menu core updates (Gamerule Icons & Debug Status). Updates once on menu open if disabled.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EntryDisableMetrics.OnEntryValueChangedUntyped.Subscribe(OnDisableMetrics);
        EntryDisableCoreUpdates.OnEntryValueChangedUntyped.Subscribe(OnDisableCoreUpdates);
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.CVR_MenuManagerPatches));
        ApplyPatches(typeof(HarmonyPatches.ViewManagerPatches));
    }

    private void OnDisableMetrics(object arg1, object arg2)
    {
        FuckCohtml.ToggleMetrics(EntryDisableMetrics.Value);
    }

    private void OnDisableCoreUpdates(object arg1, object arg2)
    {
        FuckCohtml.ToggleCoreUpdates(EntryDisableCoreUpdates.Value);
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