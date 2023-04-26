using MelonLoader;

/**
    I know the TryCatchHell thing might be a bit exessive, but it is
    built so if a user that happens to have access to a build I do not,
    I will have a good idea of what broke and where, and what to look out
    for when updates/experimentals release. (which has happened a few times)

    It is also just in case other mods break or tweak functionality that
    could fuck with switching. Or if they try to detect switching and break...

    The VRModeSwitchTracker system is also built so I can easily & quickly make adjustments to
    components that may or may not change between builds without breaking the rest of the mod.
**/

namespace NAK.DesktopVRSwitch;

public class DesktopVRSwitchMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRSwitch";
    internal static MelonPreferences_Category mCategory;
    internal static MelonLogger.Instance Logger;

    internal static MelonPreferences_Entry<bool>
        mSetting_EnterCalibrationOnSwitch;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        mCategory = MelonPreferences.CreateCategory(SettingsCategory);
        mSetting_EnterCalibrationOnSwitch = mCategory.CreateEntry<bool>("Enter Calibration on Switch", true, description: "Should you automatically be placed into calibration after switch if FBT is available? Overridden by Save Calibration IK setting.");

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRPickupObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRWorldPatches));
        ApplyPatches(typeof(HarmonyPatches.CameraFacingObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));
        ApplyPatches(typeof(HarmonyPatches.MovementSystemPatches));
        ApplyPatches(typeof(HarmonyPatches.VRTrackerManagerPatches));
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