using MelonLoader;
using NAK.Melons.MenuScalePatch.Helpers;

namespace NAK.Melons.MenuScalePatch;

public class MenuScalePatch : MelonMod
{
    private static MelonPreferences_Category m_categoryMenuScalePatch;
    private static MelonPreferences_Entry<bool> m_entryWorldAnchorVRQM;
    public override void OnInitializeMelon()
    {
        m_categoryMenuScalePatch = MelonPreferences.CreateCategory(nameof(MenuScalePatch));
        m_entryWorldAnchorVRQM = m_categoryMenuScalePatch.CreateEntry<bool>("World Anchor VR QM", false, description: "Should place QM in World Space while VR.");

        foreach (var setting in m_categoryMenuScalePatch.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }
    }

    private void UpdateAllSettings()
    {
        MSP_MenuInfo.WorldAnchorQM = m_entryWorldAnchorVRQM.Value;
    }
    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
}