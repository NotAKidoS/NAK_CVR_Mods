using MelonLoader;

namespace NAK.AvatarScaleMod;

public class AvatarScaleMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarScaleMod));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle AvatarScaleMod entirely.");

    public static readonly MelonPreferences_Entry<bool> EntryUseScaleGesture =
        Category.CreateEntry("Scale Gesture", false, description: "Use two fists to scale yourself easily.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        ModSettings.InitializeModSettings();
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.GesturePlaneTestPatches));
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