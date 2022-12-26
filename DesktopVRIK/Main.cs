using ABI_RC.Core.Player;
using MelonLoader;

namespace DesktopVRIK;

public class DesktopVRIKMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRIK";
    private static MelonPreferences_Category m_categoryDesktopVRIK;
    private static MelonPreferences_Entry<bool> m_entryEnabled, m_entryEmulateHipMovement, m_entryEmoteVRIK, m_entryEmoteLookAtIK;
    public override void OnInitializeMelon()
    {
        m_categoryDesktopVRIK = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryEnabled = m_categoryDesktopVRIK.CreateEntry<bool>("Enabled", true, description: "Attempt to give Desktop VRIK on avatar load.");
        m_entryEmulateHipMovement = m_categoryDesktopVRIK.CreateEntry<bool>("Emulate Hip Movement", true, description: "Emulates VRChat-like hip movement when moving head up/down on desktop.");
        m_entryEmoteVRIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote VRIK", true, description: "Disable VRIK while emoting. Only disable if you are ok with looking dumb.");
        m_entryEmoteLookAtIK = m_categoryDesktopVRIK.CreateEntry<bool>("Disable Emote LookAtIK", true, description: "Disable LookAtIK while emoting. This setting doesn't really matter, as LookAtIK isn't networked while doing an emote.");
        m_categoryDesktopVRIK.SaveToFile(false);

        foreach (var setting in m_categoryDesktopVRIK.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

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
        DesktopVRIK.Instance.Setting_Enabled = m_entryEnabled.Value;
        DesktopVRIK.Instance.Setting_EmulateVRChatHipMovement = m_entryEmulateHipMovement.Value;
        DesktopVRIK.Instance.Setting_EmoteVRIK = m_entryEmoteVRIK.Value;
        DesktopVRIK.Instance.Setting_EmoteLookAtIK = m_entryEmoteLookAtIK.Value;
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
}