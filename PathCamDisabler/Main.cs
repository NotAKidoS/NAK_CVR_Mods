using ABI_RC.Core.IO;
using MelonLoader;

namespace NAK.PathCamDisabler;

public class PathCamDisabler : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PathCamDisabler));

    public static readonly MelonPreferences_Entry<bool> EntryDisablePathCam =
        Category.CreateEntry("Disable Path Camera Controller", true, description: "Should the Pathing Camera Controller be disabled?");

    public static readonly MelonPreferences_Entry<bool> EntryDisableFlightBind =
        Category.CreateEntry("Disable Keyboard Flight Bind", true, description: "Should the Keyboard flight bind be disabled?");

    public override void OnInitializeMelon()
    {
        EntryDisablePathCam.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        ApplyPatches(typeof(HarmonyPatches.CVRPathCamControllerPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRInputModule_KeyboardPatches));
    }

    internal static void OnUpdateSettings(object arg1 = null, object arg2 = null)
    {
        if (CVRPathCamController.Instance != null)
            CVRPathCamController.Instance.enabled = !EntryDisablePathCam.Value;
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