using MelonLoader;

namespace NAK.AlternateIKSystem;

public static class ModSettings
{
    internal const string SettingsCategory = nameof(AlternateIKSystem);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    // Shared Settings

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true,
            description: "Toggle AlternateIKSystem entirely. Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryUseToesForVRIK =
        Category.CreateEntry("Use VRIK Toes", false,
            description:
            "Determines if VRIK uses humanoid toes for IK solving, which can cause feet to idle behind the avatar.");

    public static readonly MelonPreferences_Entry<bool> EntryPlantFeet =
        Category.CreateEntry("Enforce Plant Feet", true,
            description: "Forces VRIK Plant Feet enabled to prevent hovering when stopping movement.");

    public static readonly MelonPreferences_Entry<bool> EntryResetFootstepsOnIdle =
        Category.CreateEntry("Reset Footsteps on Idle", false,
            description:
            "Determines if the Locomotion Footsteps will be reset to their calibration position when entering idle.");

    public static readonly MelonPreferences_Entry<float> EntryIKLerpSpeed =
        Category.CreateEntry("IK Lerp Speed", 10f,
            description: "Determines fast the IK & Locomotion weights blend after entering idle. Set to 0 to disable.");

    // Desktop Settings

    public static readonly MelonPreferences_Entry<float> EntryBodyLeanWeight =
        Category.CreateEntry("Body Lean Weight", 0.5f,
            description: "Adds rotational influence to the body solver when looking up/down. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryBodyHeadingLimit =
        Category.CreateEntry("Body Heading Limit", 20f,
            description:
            "Specifies the maximum angle the lower body can have relative to the head when rotating. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryPelvisHeadingWeight =
        Category.CreateEntry("Pelvis Heading Weight", 0.25f,
            description:
            "Determines how much the pelvis will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<float> EntryChestHeadingWeight =
        Category.CreateEntry("Chest Heading Weight", 0.75f,
            description:
            "Determines how much the chest will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<bool> EntryProneThrusting =
        Category.CreateEntry("Prone Thrusting", false,
            description: "Allows Body Lean Weight to take effect while crouched or prone.");
}