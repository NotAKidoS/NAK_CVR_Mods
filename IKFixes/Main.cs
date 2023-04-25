using MelonLoader;

namespace NAK.IKFixes;

public class IKFixes : MelonMod
{
    public const string SettingsCategory = nameof(IKFixes);
    public static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryUseFakeRootAngle =
        Category.CreateEntry("Use Fake Root Angle", true, description: "Emulates maxRootAngle. This fixes feet pointing in direction of head when looking around.");

    public static readonly MelonPreferences_Entry<float> EntryFakeRootAngleLimit =
        Category.CreateEntry("Fake Root Angle Limit", 25f, description: "Specifies the maximum angle the lower body can have relative to the head when rotating.");

    public static readonly MelonPreferences_Entry<float> EntryNeckStiffness =
        Category.CreateEntry("Neck Stiffness", 0.2f, description: "Neck stiffness.");

    public static readonly MelonPreferences_Entry<float> EntryBodyRotStiffness =
        Category.CreateEntry("Body Rot Stiffness", 0.1f, description: "Body rotation stiffness.");

    public static readonly MelonPreferences_Entry<float> EntryRotateChestByHands =
        Category.CreateEntry("Rot Chest By Hands", 1f, description: "Rotate chest by hands.");

    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.VRIKPatches));
        ApplyPatches(typeof(HarmonyPatches.BodySystemPatches));
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
    }

    void ApplyPatches(Type type)
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