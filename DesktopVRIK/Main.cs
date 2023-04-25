using MelonLoader;
using UnityEngine;

namespace NAK.DesktopVRIK;

public class DesktopVRIKMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    public const string SettingsCategory = "DesktopVRIK";
    public static readonly MelonPreferences_Category CategoryDesktopVRIK = MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        CategoryDesktopVRIK.CreateEntry("Enabled", true, description: "Toggle DesktopVRIK entirely. Requires avatar reload.");

    public static readonly MelonPreferences_Entry<bool> EntryPlantFeet =
        CategoryDesktopVRIK.CreateEntry("Enforce Plant Feet", true, description: "Forces VRIK Plant Feet enabled to prevent hovering when stopping movement.");

    public static readonly MelonPreferences_Entry<bool> EntryResetFootstepsOnIdle =
        CategoryDesktopVRIK.CreateEntry("Reset Footsteps on Idle", true, description: "Determins if the Locomotion Footsteps will be reset to their calibration position when entering idle.");

    public static readonly MelonPreferences_Entry<bool> EntryUseVRIKToes =
        CategoryDesktopVRIK.CreateEntry("Use VRIK Toes", false, description: "Determines if VRIK uses humanoid toes for IK solving, which can cause feet to idle behind the avatar.");

    public static readonly MelonPreferences_Entry<bool> EntryFindUnmappedToes =
        CategoryDesktopVRIK.CreateEntry("Find Unmapped Toes", false, description: "Determines if DesktopVRIK should look for unmapped toe bones if the humanoid rig does not have any.");

    public static readonly MelonPreferences_Entry<float> EntryBodyLeanWeight =
        CategoryDesktopVRIK.CreateEntry("Body Lean Weight", 0.5f, description: "Adds rotational influence to the body solver when looking up/down. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryBodyHeadingLimit =
        CategoryDesktopVRIK.CreateEntry("Body Heading Limit", 20f, description: "Specifies the maximum angle the lower body can have relative to the head when rotating. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<float> EntryPelvisHeadingWeight =
        CategoryDesktopVRIK.CreateEntry("Pelvis Heading Weight", 0.25f, description: "Determines how much the pelvis will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<float> EntryChestHeadingWeight =
        CategoryDesktopVRIK.CreateEntry("Chest Heading Weight", 0.75f, description: "Determines how much the chest will face the Body Heading Limit. Set to 0 to align with head.");

    public static readonly MelonPreferences_Entry<float> EntryIKLerpSpeed =
        CategoryDesktopVRIK.CreateEntry("IK Lerp Speed", 10f, description: "Determines fast the IK & Locomotion weights blend after entering idle. Set to 0 to disable.");

    public static readonly MelonPreferences_Entry<bool> EntryProneThrusting =
        CategoryDesktopVRIK.CreateEntry("Prone Thrusting", false, description: "Allows Body Lean Weight to take affect while crouched or prone.");

    public static readonly MelonPreferences_Entry<bool> EntryIntegrationAMT =
        CategoryDesktopVRIK.CreateEntry("AMT Integration", true, description: "Relies on AvatarMotionTweaker to handle VRIK Locomotion weights if available.");

    public static bool integration_AMT = false;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        CategoryDesktopVRIK.Entries.ForEach(e => e.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings));

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));

        //BTKUILib Misc Tab
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            Logger.Msg("Initializing BTKUILib support.");
            BTKUIAddon.Init();
        }
        //AvatarMotionTweaker Handling
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "AvatarMotionTweaker"))
        {
            Logger.Msg("AvatarMotionTweaker was found. Relying on it to handle VRIK locomotion.");
            integration_AMT = true;
        }
        else
        {
            Logger.Msg("AvatarMotionTweaker was not found. Using built-in VRIK locomotion handling.");
        }
    }

    internal static void UpdateAllSettings()
    {
        if (!DesktopVRIKSystem.Instance) return;
        // DesktopVRIK Settings
        DesktopVRIKSystem.Instance.Setting_Enabled = EntryEnabled.Value;
        DesktopVRIKSystem.Instance.Setting_PlantFeet = EntryPlantFeet.Value;
        DesktopVRIKSystem.Instance.Setting_ResetFootsteps = EntryResetFootstepsOnIdle.Value;

        DesktopVRIKSystem.Instance.Setting_BodyLeanWeight = Mathf.Clamp01(EntryBodyLeanWeight.Value);
        DesktopVRIKSystem.Instance.Setting_BodyHeadingLimit = Mathf.Clamp(EntryBodyHeadingLimit.Value, 0f, 90f);
        DesktopVRIKSystem.Instance.Setting_PelvisHeadingWeight = (1f - Mathf.Clamp01(EntryPelvisHeadingWeight.Value));
        DesktopVRIKSystem.Instance.Setting_ChestHeadingWeight = (1f - Mathf.Clamp01(EntryChestHeadingWeight.Value));
        DesktopVRIKSystem.Instance.Setting_ChestHeadingWeight = (1f - Mathf.Clamp01(EntryChestHeadingWeight.Value));
        DesktopVRIKSystem.Instance.Setting_IKLerpSpeed = Mathf.Clamp(EntryIKLerpSpeed.Value, 0f, 20f);

        // Calibration Settings
        DesktopVRIKSystem.Instance.Setting_UseVRIKToes = EntryUseVRIKToes.Value;
        DesktopVRIKSystem.Instance.Setting_FindUnmappedToes = EntryFindUnmappedToes.Value;

        // Fine-tuning Settings
        DesktopVRIKSystem.Instance.Setting_ResetFootsteps = EntryResetFootstepsOnIdle.Value;

        // Integration Settings
        DesktopVRIKSystem.Instance.Setting_IntegrationAMT = EntryIntegrationAMT.Value && integration_AMT;
        
        // Funny Settings
        DesktopVRIKSystem.Instance.Setting_ProneThrusting = EntryProneThrusting.Value;
    }
    void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();

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