using MelonLoader;
using System.Reflection;
using ABI_RC.Systems.InputManagement.InputModules;
using ABI_RC.Systems.InputManagement.XR;

namespace NAK.EzGrab;

public class EzGrab : MelonMod
{
    private const string SettingsCategory = nameof(EzGrab);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, 
            description: "Should EzGrab be enabled?");
    
    private static readonly MelonPreferences_Entry<float> EntryReleaseWeight =
        Category.CreateEntry("Release Weight", 0.1f, 
            description: "The release weight for grip up to fire.");

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(CVRInputModule_XR).GetMethod(nameof(CVRInputModule_XR.Update_Grip)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(EzGrab).GetMethod(nameof(OnCVRInputModule_XR_Prefix), BindingFlags.NonPublic))
        );
    }

    private static void OnCVRInputModule_XR_Prefix(CVRXRModule module, ref CVRInputModule_XR __instance)
    {
        if (module.Type != EXRControllerType.Index || !EntryEnabled.Value) return;

        float gripValue = module.Grip;
        float gripThreshold = __instance._indexGripThreshold;
        float releaseThreshold = EntryReleaseWeight.Value;

        if (module.IsLeftHand)
        {
            float lastGripLeft = __instance._lastGripLeft;
            if ((gripValue < gripThreshold && lastGripLeft >= gripThreshold) &&
                !(gripValue < releaseThreshold && lastGripLeft >= releaseThreshold))
            {
                __instance._lastGripLeft = 0f;
            }
        }
        else
        {
            float lastGripRight = __instance._lastGripRight;
            if ((gripValue < gripThreshold && lastGripRight >= gripThreshold) &&
                !(gripValue < releaseThreshold && lastGripRight >= releaseThreshold))
            {
                __instance._lastGripRight = 0f;
            }
        }
    }
}