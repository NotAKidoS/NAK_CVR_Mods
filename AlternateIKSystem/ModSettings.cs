using MelonLoader;

namespace NAK.AlternateIKSystem;

public static class ModSettings
{
    internal const string SettingsCategory = nameof(AlternateIKSystem);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle AlternateIKSystem entirely. Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryUseVRIKToes =
        Category.CreateEntry("Use VRIK Toes", false, description: "Determines if VRIK uses humanoid toes for IK solving, which can cause feet to idle behind the avatar.");
}