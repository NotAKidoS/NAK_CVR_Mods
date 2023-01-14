using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace NAK.Melons.DesktopVRIK;

public class DesktopVRIKMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRIK";
    internal static MelonPreferences_Category m_categoryDesktopVRIK;
    internal static MelonPreferences_Entry<bool> m_entryEnabled,
        m_entryEnforceViewPosition,
        m_entryEmoteVRIK,
        m_entryEmoteLookAtIK,
        m_entryAllowRootSlipping;
    internal static MelonPreferences_Entry<float> m_entryEmulateVRChatHipMovementWeight;
    public override void OnInitializeMelon()
    {
        m_categoryDesktopVRIK = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryEnabled = m_categoryDesktopVRIK.CreateEntry<bool>("Enabled", true, description: "Toggle DesktopVRIK entirely. Requires avatar reload.");
        m_entryEmulateVRChatHipMovementWeight = m_categoryDesktopVRIK.CreateEntry<float>("Body Movement Weight", 0.5f, description: "Emulates VRChat-like body movement when looking up/down. Set to 0 to disable.");
        m_entryEnforceViewPosition = m_categoryDesktopVRIK.CreateEntry<bool>("Enforce View Position", false, description: "Corrects view position to use VRIK offsets.");
        m_entryEmoteVRIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote VRIK", true, description: "Disable VRIK while emoting. Only disable if you are ok with looking dumb.");
        m_entryEmoteLookAtIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote LookAtIK", true, description: "Disable LookAtIK while emoting. This setting doesn't really matter, as LookAtIK isn't networked while doing an emote.");

        foreach (var setting in m_categoryDesktopVRIK.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        //BTKUILib Misc Support
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
        {
            MelonLogger.Msg("Initializing BTKUILib support.");
            BTKUI_Integration.BTKUI_Integration.Init();
        }

        //Apply patches (i stole)
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));

        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;
        PlayerSetup.Instance.gameObject.AddComponent<DesktopVRIK>();
        while (DesktopVRIK.Instance == null)
            yield return null;
        UpdateAllSettings();
    }

    private void UpdateAllSettings()
    {
        if (!DesktopVRIK.Instance) return;
        DesktopVRIK.Setting_Enabled = m_entryEnabled.Value;
        DesktopVRIK.Setting_EmulateVRChatHipMovementWeight = Mathf.Clamp01(m_entryEmulateVRChatHipMovementWeight.Value);
        DesktopVRIK.Setting_EmoteVRIK = m_entryEmoteVRIK.Value;
        DesktopVRIK.Setting_EmoteLookAtIK = m_entryEmoteLookAtIK.Value;
        DesktopVRIK.Instance.ChangeViewpointHandling(m_entryEnforceViewPosition.Value);
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();

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