using MelonLoader;

namespace NAK.Melons.DesktopVRSwitch;

public class DesktopVRSwitchMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRSwitch";
    internal static MelonPreferences_Category m_categoryDesktopVRSwitch;
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        m_categoryDesktopVRSwitch = MelonPreferences.CreateCategory(SettingsCategory);

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRPickupObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRWorldPatches));
        ApplyPatches(typeof(HarmonyPatches.CameraFacingObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));
        ApplyPatches(typeof(HarmonyPatches.MovementSystemPatches));
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