using MelonLoader;

namespace NAK.DesktopVRSwitch;

public static class ModSettings
{
    internal const string SettingsCategory = nameof(DesktopVRSwitch);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    public static readonly MelonPreferences_Entry<bool> EntryEnterCalibrationOnSwitch =
        Category.CreateEntry("Enter Calibration on Switch", true, description: "Should you automatically be placed into calibration after switch if FBT is available? Overridden by Save Calibration IK setting.");

    public static readonly MelonPreferences_Entry<bool> EntryUseTransitionOnSwitch =
        Category.CreateEntry("Use Transition on Switch", true, description: "Should the world transition play on VRMode switch?");
    
    public static readonly MelonPreferences_Entry<bool> EntrySwitchToDesktopOnExit =
        Category.CreateEntry("Switch to Desktop on SteamVR Exit", false, description: "Should the game switch to Desktop when SteamVR quits?");
}