using MelonLoader;

namespace NAK.BadAnimatorFix;

public class BadAnimatorFix : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(BadAnimatorFix));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle BadAnimatorFix entirely. Requires avatar/spawnable/world reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRAvatar =
        Category.CreateEntry("Add to CVRAvatar", true, description: "Should BadAnimatorFix run for CVRAvatar? Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRSpawnable =
        Category.CreateEntry("Add to CVRSpawnable", true, description: "Should BadAnimatorFix run for CVRSpawnable? Requires spawnable reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRWorld =
        Category.CreateEntry("Add to CVRWorld", true, description: "Should BadAnimatorFix run for CVRWorld? Requires world reload.");

    public static readonly MelonPreferences_Entry<bool> EntryLogging =
        Category.CreateEntry("Debugging", false, description: "Toggle to log each rewind if successful. Only needed for debugging.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(HarmonyPatches.AnimatorPatches));
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (!EntryCVRWorld.Value) return;

        if (buildIndex < 0) // -1 is custom world: 0 to 3 is game login, init, hq
            BadAnimatorFixManager.OnSceneInitialized(sceneName);
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