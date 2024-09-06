using MelonLoader;

namespace NAK.MoreMenuOptions;

public static class ModSettings
{
    private const string SettingsCategory = nameof(MoreMenuOptions);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    // main menu options
    
    internal static readonly MelonPreferences_Entry<MoreMenuOptions.MainMenuModifierUsage> EntryMainMenuModiferUsage =
        Category.CreateEntry("Main Menu Modifier Usage", MoreMenuOptions.MainMenuModifierUsage.Both, description: "The usage of the main menu modifier.");
    
    internal static readonly MelonPreferences_Entry<float> EntryMMScaleModifier =
        Category.CreateEntry("Main Menu Scale Modifier", 0.75f, description: "The scale of the main menu.");
    
    internal static readonly MelonPreferences_Entry<float> EntryMMDistanceModifier =
        Category.CreateEntry("Main Menu Distance Modifier", 0.1f, description: "The distance of the main menu from the camera.");
    
    // quick menu options
    
    internal static readonly MelonPreferences_Entry<bool> EntryQMWorldAnchorInVR =
        Category.CreateEntry("Quick Menu World Anchor In VR", false, description: "Toggle the quick menu world anchor in VR.");
}