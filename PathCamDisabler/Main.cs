using MelonLoader;

namespace NAK.PathCamDisabler;

public class PathCamDisabler : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PathCamDisabler));

    public static readonly MelonPreferences_Entry<bool> EntryDisablePathCam =
        Category.CreateEntry("Disable Path Camera Controller", true, description: "Disable Path Camera Controller.");

    public static readonly MelonPreferences_Entry<bool> EntryDisableFlightBind =
        Category.CreateEntry("Disable Flight Binding", false, description: "Disable flight bind if Path Camera Controller is also disabled.");

    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.CVRPathCamControllerPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRInputModule_KeyboardPatches));
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