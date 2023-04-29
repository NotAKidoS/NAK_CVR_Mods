using MelonLoader;

namespace NAK.AvatarScaleMod;

public class AvatarScaleMod : MelonMod
{
    internal const string ParameterName = "AvatarScale";
    internal const float MinimumHeight = 0.25f;
    internal const float MaximumHeight = 2.5f;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarScaleMod));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Should there be persistant avatar scaling? This only works properly across supported avatars.");

    public static readonly MelonPreferences_Entry<bool> EntryPersistAnyways =
        Category.CreateEntry("Persist From Unsupported", true, description: "Should avatar scale persist even from unsupported avatars?");

    public static readonly MelonPreferences_Entry<float> HiddenAvatarScale =
        Category.CreateEntry("Last Avatar Scale", -1f, is_hidden: true);

    public override void OnInitializeMelon()
    {
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