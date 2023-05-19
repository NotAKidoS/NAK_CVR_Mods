using MelonLoader;

namespace NAK.DesktopVRIK;

public class DesktopVRIK : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal const string SettingsCategory = nameof(DesktopVRIK);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle DesktopVRIK entirely. Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryPlantFeet =
        Category.CreateEntry("Enforce Plant Feet", true, description: "Forces VRIK Plant Feet enabled to prevent hovering when stopping movement.");

    public static readonly MelonPreferences_Entry<bool> EntryResetFootstepsOnIdle =
        Category.CreateEntry("Reset Footsteps on Idle", false, description: "Determins if the Locomotion Footsteps will be reset to their calibration position when entering idle.");

    public static readonly MelonPreferences_Entry<bool> EntryUseVRIKToes =
        Category.CreateEntry("Use VRIK Toes", false, description: "Determines if VRIK uses humanoid toes for IK solving, which can cause feet to idle behind the avatar.");

    public static readonly MelonPreferences_Entry<float> EntryBodyLeanWeight =
        Category.CreateEntry("Body Lean Weight", 0.5f, description: "Adds rotational influence to the body solver when looking up/down. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryBodyHeadingLimit =
        Category.CreateEntry("Body Heading Limit", 20f, description: "Specifies the maximum angle the lower body can have relative to the head when rotating. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryPelvisHeadingWeight =
        Category.CreateEntry("Pelvis Heading Weight", 0.25f, description: "Determines how much the pelvis will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<float> EntryChestHeadingWeight =
        Category.CreateEntry("Chest Heading Weight", 0.75f, description: "Determines how much the chest will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<float> EntryIKLerpSpeed =
        Category.CreateEntry("IK Lerp Speed", 10f, description: "Determines fast the IK & Locomotion weights blend after entering idle. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<bool> EntryProneThrusting =
        Category.CreateEntry("Prone Thrusting", false, description: "Allows Body Lean Weight to take effect while crouched or prone.");

    public static readonly MelonPreferences_Entry<bool> EntryNetIKPass =
        Category.CreateEntry("Network IK Pass", true, description: "Should NetIK pass be applied? This fixes a bunch of small rotation errors after VRIK is run.");

    public static readonly MelonPreferences_Entry<bool> EntryIntegrationAMT =
        Category.CreateEntry("AMT Integration", true, description: "Relies on AvatarMotionTweaker to handle VRIK Locomotion weights if available.");

    public static bool integration_AMT = false;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        //BTKUILib Misc Tab
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            Logger.Msg("Initializing BTKUILib support.");
            Integrations.BTKUIAddon.Init();
        }

        //AvatarMotionTweaker Handling
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "AvatarMotionTweaker"))
        {
            integration_AMT = true;
            Logger.Msg("AvatarMotionTweaker was found. Relying on it to handle VRIK locomotion.");
        }
        else
        {
            Logger.Msg("AvatarMotionTweaker was not found. Using built-in VRIK locomotion handling.");
        }

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
    }

    void ApplyPatches(Type type)
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