using cohtml;
using MelonLoader;
using UnityEngine;

namespace NAK.DummyMenu;

public static class ModSettings
{
    private static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory(nameof(DummyMenu));
    
    internal static readonly MelonPreferences_Entry<KeyCode> EntryToggleDummyMenu =
        Category.CreateEntry(
            identifier: "toggle_dummy_menu", 
            default_value: KeyCode.F3,
            display_name: "Toggle Menu Key",
            description: "Key used to toggle the dummy menu.");
    
    internal static readonly MelonPreferences_Entry<string> EntryPageCouiPath =
        Category.CreateEntry(
            identifier: "page_coui_path", 
            default_value: "UIResources/DummyMenu/menu.html",
            display_name: "Page Coui Path", 
            description: "Path to the folder containing the root menu html. This is relative to the StreamingAssets folder.");
    
    internal static readonly MelonPreferences_Entry<int> EntryPageWidth =
        Category.CreateEntry("page_width", CohtmlView.DefaultWidth, 
            display_name: "Page Width",
            description: "Width of the menu page in pixels. Default is 1280 pixels.");
    
    internal static readonly MelonPreferences_Entry<int> EntryPageHeight =
        Category.CreateEntry("page_height", CohtmlView.DefaultHeight,
            display_name: "Page Height", 
            description: "Height of the menu page in pixels. Default is 720 pixels.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryReloadMenuEvenWhenOpen =
        Category.CreateEntry("reload_menu_even_when_open", false,
            display_name: "Reload Menu Even When Open",
            description: "If enabled, the menu will be reloaded even if it is already open.");

    internal static readonly MelonPreferences_Entry<float> EntryVrMenuScaleModifier =
        Category.CreateEntry("vr_menu_scale_modifier", 0.75f,
            display_name: "VR Menu Scale Modifier",
            description: "Adjusts the scale of the menu while in VR. Default is 0.75.");

    internal static readonly MelonPreferences_Entry<float> EntryDesktopMenuScaleModifier =
        Category.CreateEntry("desktop_menu_scale_modifier", 1f,
            display_name: "Desktop Menu Scale Modifier",
            description: "Adjusts the scale of the menu while in Desktop mode. Default is 1.");

    internal static readonly MelonPreferences_Entry<float> EntryVrMenuDistanceModifier =
        Category.CreateEntry("vr_menu_distance_modifier", 1f,
            display_name: "VR Menu Distance Modifier",
            description: "Adjusts the distance of the menu from the camera while in VR. Default is 1.");

    internal static readonly MelonPreferences_Entry<float> EntryDesktopMenuDistanceModifier =
        Category.CreateEntry("desktop_menu_distance_modifier", 1.2f,
            display_name: "Desktop Menu Distance Modifier",
            description: "Adjusts the distance of the menu from the camera while in Desktop mode. Default is 1.2.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryToggleMeToResetModifiers =
        Category.CreateEntry("toggle_me_to_reset_modifiers", false,
            display_name: "Toggle Me To Reset Modifiers",
            description: "If enabled, toggling the menu will reset any scale/distance modifiers applied by other mods.");
}