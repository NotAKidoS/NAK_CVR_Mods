using MelonLoader;

namespace NAK.PropsButBetter;

public static class ModSettings
{
    public const string ModName = nameof(PropsButBetter);
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);

    internal static readonly MelonPreferences_Entry<bool> EntryUseUndoRedoKeybinds =
        Category.CreateEntry("use_undo_redo_keybinds", true, 
            "Use Undo/Redo Keybinds", description: "Whether to use the Desktop keybinds to undo/redo Props (CTRL+Z, CTRL+SHIFT+Z).");
    
    internal static readonly MelonPreferences_Entry<bool> EntryUseSFX =
        Category.CreateEntry("use_sfx", true, 
            "Use SFX", description: "Toggle audio queues for prop spawn, undo, redo, and warning.");

    internal static readonly MelonPreferences_Entry<QuickMenuPropList.PropListMode> HiddenPropListMode =
        Category.CreateEntry("prop_list_mode", QuickMenuPropList.PropListMode.AllProps,
            "Prop List Mode", description: "The current prop list mode.", is_hidden: true);
    
    internal static readonly MelonPreferences_Entry<bool> EntryFixPlayerSelectRedirect =
        Category.CreateEntry("fix_player_select_redirect", true, 
            "Fix Player Select Redirect", description: "Whether to fix the player select redirect. Enabling this makes it consistent with Prop Select.");

    internal static readonly MelonPreferences_Entry<bool> EntryPropSpawnVisualizer =
        Category.CreateEntry("prop_spawn_visualizer", true,
            "Prop Spawn Visualizer", description: "Use the experimental probably fucked up prop spawn visualizer.");
    
    internal static readonly MelonPreferences_Entry<PropHelper.PropVisualizerMode> EntryPropSpawnVisualizerMode =
        Category.CreateEntry("prop_spawn_visualizer_mode", PropHelper.PropVisualizerMode.HologramSource,
            "Prop Spawn Visualizer Mode", description: "Choose how the visualizer will attempt to render.");
}