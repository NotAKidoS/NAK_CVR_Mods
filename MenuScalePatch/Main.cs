using MelonLoader;
using NAK.Melons.MenuScalePatch.Helpers;

namespace NAK.Melons.MenuScalePatch;

public class MenuScalePatch : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal static MelonPreferences_Category m_categoryMenuScalePatch;
    internal static MelonPreferences_Entry<bool> 
        m_entryWorldAnchorVRQM,
        m_entryUseIndependentHeadTurn,
        m_entryPlayerAnchorMenus;
    public override void OnInitializeMelon()
    {
        m_categoryMenuScalePatch = MelonPreferences.CreateCategory(nameof(MenuScalePatch));
        //m_entryWorldAnchorVRQM = m_categoryMenuScalePatch.CreateEntry<bool>("World Anchor VR QM", false, description: "Should place QM in World Space while VR.");
        m_entryUseIndependentHeadTurn = m_categoryMenuScalePatch.CreateEntry<bool>("Use Independent Head Turn", true, description: "Should you be able to use independent head turn in a menu while in Desktop?");
        m_entryPlayerAnchorMenus = m_categoryMenuScalePatch.CreateEntry<bool>("Player Anchor Menus", true, description: "Should the menus be anchored to & constantly follow the player?");
        
        foreach (var setting in m_categoryMenuScalePatch.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        Logger = LoggerInstance;
    }
    internal static void UpdateAllSettings()
    {
        //MSP_MenuInfo.WorldAnchorQM = m_entryWorldAnchorVRQM.Value;
        MSP_MenuInfo.UseIndependentHeadTurn = m_entryUseIndependentHeadTurn.Value;
        MSP_MenuInfo.PlayerAnchorMenus = m_entryPlayerAnchorMenus.Value;
    }
    private static void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
}