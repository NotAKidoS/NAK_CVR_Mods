using MelonLoader;
using UnityEngine;

namespace NAK.Melons.DesktopVRIK;

public class DesktopVRIKMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRIK";
    internal static MelonPreferences_Category m_categoryDesktopVRIK;
    internal static MelonPreferences_Entry<bool>
        m_entryEnabled,
        m_entryEnforceViewPosition,
        m_entryResetIKOnLand,
        m_entryPlantFeet,
        m_entryUseVRIKToes,
        m_entryFindUnmappedToes;
    internal static MelonPreferences_Entry<float>
        m_entryBodyLeanWeight,
        m_entryBodyHeadingLimit,
        m_entryPelvisHeadingWeight,
        m_entryChestHeadingWeight;
    public override void OnInitializeMelon()
    {
        m_categoryDesktopVRIK = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryEnabled = m_categoryDesktopVRIK.CreateEntry<bool>("Enabled", true, description: "Toggle DesktopVRIK entirely. Requires avatar reload.");
        m_entryEnforceViewPosition = m_categoryDesktopVRIK.CreateEntry<bool>("Enforce View Position", false, description: "Corrects view position to use VRIK offsets.");
        m_entryResetIKOnLand = m_categoryDesktopVRIK.CreateEntry<bool>("Reset IK On Land", true, description: "Reset Solver IK when landing on the ground.");
        m_entryPlantFeet = m_categoryDesktopVRIK.CreateEntry<bool>("Enforce Plant Feet", true, description: "Forces VRIK Plant Feet enabled. This prevents the little hover when you stop moving.");
        m_entryUseVRIKToes = m_categoryDesktopVRIK.CreateEntry<bool>("Use VRIK Toes", false, description: "Should VRIK use your humanoid toes for IK solving? This can cause your feet to idle behind you.");
        m_entryFindUnmappedToes = m_categoryDesktopVRIK.CreateEntry<bool>("Find Unmapped Toes", false, description: "Should DesktopVRIK look for unmapped toe bones if humanoid rig does not have any?");

        m_entryBodyLeanWeight = m_categoryDesktopVRIK.CreateEntry<float>("Body Lean Weight", 0.5f, description: "Emulates old VRChat-like body leaning when looking up/down. Set to 0 to disable.");
        m_entryBodyHeadingLimit = m_categoryDesktopVRIK.CreateEntry<float>("Body Heading Limit", 20f, description: "Emulates VRChat-like body and head offset when rotating left/right. Set to 0 to disable.");
        m_entryPelvisHeadingWeight = m_categoryDesktopVRIK.CreateEntry<float>("Pelvis Heading Weight", 0.25f, description: "How much the pelvis will face the heading limit. Set to 0 to align with head.");
        m_entryChestHeadingWeight = m_categoryDesktopVRIK.CreateEntry<float>("Chest Heading Weight", 0.75f, description: "How much the chest will face the heading limit. Set to 0 to align with head.");

        foreach (var setting in m_categoryDesktopVRIK.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));

        InitializeIntegrations();
    }

    internal static void UpdateAllSettings()
    {
        if (!DesktopVRIK.Instance) return;
        // DesktopVRIK Settings
        DesktopVRIK.Instance.Setting_Enabled = m_entryEnabled.Value;
        DesktopVRIK.Instance.Setting_BodyLeanWeight = Mathf.Clamp01(m_entryBodyLeanWeight.Value);
        DesktopVRIK.Instance.Setting_ResetOnLand = m_entryResetIKOnLand.Value;
        DesktopVRIK.Instance.Setting_PlantFeet = m_entryPlantFeet.Value;
        DesktopVRIK.Instance.Setting_BodyHeadingLimit = Mathf.Clamp(m_entryBodyHeadingLimit.Value, 0f, 90f);
        DesktopVRIK.Instance.Setting_PelvisHeadingWeight = (1f - Mathf.Clamp01(m_entryPelvisHeadingWeight.Value));
        DesktopVRIK.Instance.Setting_ChestHeadingWeight = (1f - Mathf.Clamp01(m_entryChestHeadingWeight.Value));
        // Calibration Settings
        DesktopVRIK.Instance.Calibrator.Setting_UseVRIKToes = m_entryUseVRIKToes.Value;
        DesktopVRIK.Instance.Calibrator.Setting_FindUnmappedToes = m_entryFindUnmappedToes.Value;
    }
    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();

    private static void InitializeIntegrations()
    {
        //BTKUILib Misc Tab
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            MelonLogger.Msg("Initializing BTKUILib support.");
            //BTKUIAddon.Init();
        }
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