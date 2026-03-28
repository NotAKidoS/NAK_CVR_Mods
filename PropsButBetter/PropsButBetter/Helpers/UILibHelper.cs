using System.Reflection;
using ABI_RC.Systems.UI.UILib;

namespace NAK.PropsButBetter;

public static class UILibHelper
{
    public static string PlaceholderImageCoui => GetIconCoui(ModSettings.ModName, $"{ModSettings.ModName}-placeholder");
    
    public static string GetIconCoui(string modName, string iconName)
    {
        modName = UIUtils.GetCleanString(modName);
        return $"coui://uiresources/GameUI/UILib/Images/{modName}/{iconName}.png";
    }

    internal static void LoadIcons()
    {
        // Load all icons
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name;
        
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-remove", GetIconStream("remove.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-reload", GetIconStream("reload.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-select", GetIconStream("select.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-undo", GetIconStream("undo.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-redo", GetIconStream("redo.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-wand", GetIconStream("wand.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-rubiks-cube", GetIconStream("rubiks-cube.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-rubiks-cube-eye", GetIconStream("rubiks-cube-eye.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-rubiks-cube-star", GetIconStream("rubiks-cube-star.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-rubiks-cube-clock", GetIconStream("rubiks-cube-clock.png"));
        QuickMenuAPI.PrepareIcon(ModSettings.ModName, $"{ModSettings.ModName}-placeholder", GetIconStream("placeholder.png"));
        Stream GetIconStream(string iconName) => assembly.GetManifestResourceStream($"{assemblyName}.Resources.{iconName}");
    }
}