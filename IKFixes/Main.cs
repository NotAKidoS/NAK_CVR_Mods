using MelonLoader;

namespace NAK.Melons.IKFixes;

public class IKFixesMod : MelonMod
{
    public const string SettingsCategory = "IKFixes";
    public static readonly MelonPreferences_Category CategoryIKFixes = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryUseFakeRootAngle =
        CategoryIKFixes.CreateEntry("Use Fake Root Angle", false, description: "Emulates maxRootAngle. This fixes feet pointing in direction of head when looking around.");

    public static readonly MelonPreferences_Entry<float> EntryFakeRootAngleLimit =
        CategoryIKFixes.CreateEntry("Fake Root Angle Limit", 25f, description: "Specifies the maximum angle the lower body can have relative to the head when rotating.");

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