using MelonLoader;
using System.Collections;
using ABI_RC.Core.Player;

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
        CategoryBadAnimatorFix.CreateEntry("Add to CVRSpawnable", false, description: "Should BadAnimatorFix run for CVRSpawnable? Requires spawnable reload.");

    public static readonly MelonPreferences_Entry<bool> EntryCVRWorld =
        CategoryBadAnimatorFix.CreateEntry("Add to CVRWorld", false, description: "Should BadAnimatorFix run for CVRWorld? Requires world reload.");

    public static readonly MelonPreferences_Entry<bool> EntryMenus =
        CategoryBadAnimatorFix.CreateEntry("Add to Menus", false, description: "Should BadAnimatorFix run for QM & MM? Requires game restart.");

    public static readonly MelonPreferences_Entry<float> EntryPlayableTimeLimit =
        CategoryBadAnimatorFix.CreateEntry("Playable Time Limit", 600f, description: "How long in seconds can a Playable play for before rewinding its states.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        EntryEnabled.OnEntryValueChangedUntyped.Subscribe(OnEnabled);
        ApplyPatches(typeof(HarmonyPatches.AnimatorPatches));
        MelonCoroutines.Start(WaitForLocalPlayer());
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;
        BadAnimatorFixManager.ToggleJob(EntryEnabled.Value);
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