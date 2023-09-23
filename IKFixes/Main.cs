using MelonLoader;

namespace NAK.IKFixes;

public class IKFixes : MelonMod
{
    internal const string SettingsCategory = nameof(IKFixes);
    
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryUseFakeRootAngle =
        Category.CreateEntry("use_fake_root_angle", true, display_name: "Use Fake Root Angle", description: "Emulates maxRootAngle. This fixes feet pointing in direction of head when looking around.");

    public static readonly MelonPreferences_Entry<float> EntryFakeRootAngleLimit =
        Category.CreateEntry("fake_root_angle_limit", 25f, display_name: "Fake Root Angle Limit (25f)", description: "Specifies the maximum angle the lower body can have relative to the head when rotating.");

    public static readonly MelonPreferences_Entry<float> EntryNeckStiffness =
        Category.CreateEntry("neck_stiffness", 0.2f, display_name: "Neck Stiffness (0.2f)", description: "Neck stiffness.");

    public static readonly MelonPreferences_Entry<float> EntryBodyRotStiffness =
        Category.CreateEntry("body_rot_stiffness", 0.1f, display_name: "Body Rot Stiffness (0.1f)", description: "Body rotation stiffness.");

    public static readonly MelonPreferences_Entry<float> EntryRotateChestByHands =
        Category.CreateEntry("rot_chest_by_hands", 1f, display_name: "Rot Chest By Hands (1f)", description: "Rotate chest by hands.");

    public static readonly MelonPreferences_Entry<float> EntryBendToTargetWeight =
        Category.CreateEntry("leg_bend_to_target", 1f, display_name: "Leg Bend To Target (1f)", description: "Leg bend to target weight");

    public static readonly MelonPreferences_Entry<bool> EntryAssignRemainingTrackers =
        Category.CreateEntry("assign_remaining_trackers", true, display_name: "Assign Remaining Trackers (true)", description: "Should the game calibrate any additional trackers as secondary trackers for already-tracked points?");

    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.BodySystemPatches));
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));
        InitializeIntegration("UI Expansion Kit", Integrations.UIExKitAddon.Initialize);
    }

    private void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        LoggerInstance.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
}