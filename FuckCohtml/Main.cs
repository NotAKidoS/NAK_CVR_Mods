using MelonLoader;

namespace NAK.Melons.FuckCohtml;

public class FuckCohtmlMod : MelonMod
{
    public static MelonLogger.Instance Logger;
    public const string SettingsCategory = "FuckCohtml";
    public static readonly MelonPreferences_Category CategoryFuckCohtml = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        CategoryFuckCohtml.CreateEntry("Enabled", true, description: "Enable FuckCohtml. This forces Cohtml to update at intervals instead of every frame while closed.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(HarmonyPatches.CohtmlViewPatches));
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