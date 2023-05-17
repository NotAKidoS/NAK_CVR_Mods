using MelonLoader;

namespace NAK.MenuScalePatch;

public class MenuScalePatch : MelonMod
{
    public static MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(MenuScalePatch));

    public static MelonPreferences_Entry<bool> EntryUseIndependentHeadTurn =
        Category.CreateEntry<bool>("Use Independent Head Turn", true, description: "Should you be able to use independent head turn in a menu while in Desktop?");

    public static MelonPreferences_Entry<bool> EntryPlayerAnchorMenus =
        Category.CreateEntry<bool>("Player Anchor Menus", true, description: "Should the menus be anchored to & constantly follow the player?");

    public override void OnInitializeMelon()
    {
        foreach (var setting in Category.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }
    }

    internal static void UpdateSettings()
    {
        Helpers.MSP_MenuInfo.UseIndependentHeadTurn = EntryUseIndependentHeadTurn.Value;
        Helpers.MSP_MenuInfo.PlayerAnchorMenus = EntryPlayerAnchorMenus.Value;
    }
    private static void OnUpdateSettings(object arg1, object arg2) => UpdateSettings();
}