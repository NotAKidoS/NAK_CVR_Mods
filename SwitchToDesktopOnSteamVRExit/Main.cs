using System.Reflection;
using ABI_RC.Systems.VRModeSwitch;
using HarmonyLib;
using MelonLoader;
using Valve.VR;

namespace NAK.SwitchToDesktopOnSteamVRExit;

public class SwitchToDesktopOnSteamVRExit : MelonMod
{
    private const string SettingsCategory = nameof(SwitchToDesktopOnSteamVRExit);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle SwitchToDesktopOnSteamVRExit entirely.");
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(SteamVR_Behaviour).GetMethod("OnQuit"),
            new HarmonyMethod(typeof(SwitchToDesktopOnSteamVRExit).GetMethod(nameof(Prefix_SteamVR_Behaviour_OnQuit),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static bool Prefix_SteamVR_Behaviour_OnQuit()
    {
        if (!EntryEnabled.Value)
            return true;
        
        // If we don't switch fast enough, SteamVR will force close.
        // World Transition might cause issues. Might need to override.
        if (VRModeSwitchManager.Instance != null)
            VRModeSwitchManager.Instance.AttemptSwitch();
        
        return false;
    }
}