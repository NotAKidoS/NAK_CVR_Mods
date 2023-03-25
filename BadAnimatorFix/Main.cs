using MelonLoader;

namespace NAK.Melons.BadAnimatorFix;

public class BadAnimatorFixMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    public const string SettingsCategory = "BadAnimatorFix";
    public static readonly MelonPreferences_Category CategoryBadAnimatorFix = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        CategoryBadAnimatorFix.CreateEntry("Enabled", true, description: "Toggle BadAnimatorFix entirely. Requires avatar/spawnable/world reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRAvatar =
        CategoryBadAnimatorFix.CreateEntry("Add to CVRAvatar", true, description: "Should BadAnimatorFix run for CVRAvatar? Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRSpawnable =
        CategoryBadAnimatorFix.CreateEntry("Add to CVRSpawnable", true, description: "Should BadAnimatorFix run for CVRSpawnable? Requires spawnable reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRWorld =
        CategoryBadAnimatorFix.CreateEntry("Add to CVRWorld", true, description: "Should BadAnimatorFix run for CVRWorld? Requires world reload.");

    public static readonly MelonPreferences_Entry<bool> EntryMenus =
        CategoryBadAnimatorFix.CreateEntry("Add to Menus", true, description: "Should BadAnimatorFix run for QM & MM? Requires game restart.");

    public static readonly MelonPreferences_Entry<bool> EntryLogging =
        CategoryBadAnimatorFix.CreateEntry("Debugging", false, description: "Toggle to log each rewind if successful. Only needed for debugging.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EntryEnabled.OnEntryValueChangedUntyped.Subscribe(OnEnabled);
        ApplyPatches(typeof(HarmonyPatches.AnimatorPatches));
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (!EntryCVRWorld.Value) return;
        BadAnimatorFixManager.OnSceneInitialized(sceneName);
    }

    private void OnEnabled(object arg1, object arg2)
    {
        BadAnimatorFixManager.ToggleJob(EntryEnabled.Value);
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