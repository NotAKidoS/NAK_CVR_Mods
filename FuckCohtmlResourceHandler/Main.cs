using MelonLoader;

namespace NAK.FuckCohtmlResourceHandler;

public class FuckCohtmlResourceHandler : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckCohtmlResourceHandler));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle FuckCohtmlResourceHandler entirely.");

    public static readonly MelonPreferences_Entry<bool> EntryBlockBadgesUrl =
        Category.CreateEntry("BlockBadgesUrl", true, description: "Toggle whether to block Badges URL.");

    public static readonly MelonPreferences_Entry<bool> EntryBlockUserImagesUrl =
        Category.CreateEntry("BlockUserImagesUrl", false, description: "Toggle whether to block User Images URL.");

    public static readonly MelonPreferences_Entry<bool> EntryBlockWorldImagesUrl =
        Category.CreateEntry("BlockWorldImagesUrl", false, description: "Toggle whether to block World Images URL.");

    public static readonly MelonPreferences_Entry<bool> EntryBlockAllUrl =
        Category.CreateEntry("BlockAllUrl", false, description: "Toggle whether to block All content URL.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        HarmonyPatches.DefaultResourceHandlerPatches.Initialize();
        ApplyPatches(typeof(HarmonyPatches.DefaultResourceHandlerPatches));
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