using MelonLoader;

namespace NAK.ShareBubbles;

// TODO:
// Setting for ShareBubbles scaling with player size
// Setting for receiving notification when a direct share is received
// Setting for ShareBubble being interactable outside of the UI buttons (Grab & Click)
// Store last Visibility, Lifetime, and Access Control settings for ShareBubble placement in hidden melon preferences

public static class ModSettings
{
    #region Constants & Category

    internal const string ModName = nameof(ShareBubbles); // TODO idea: BTKUI player page button to remove player's ShareBubbles ?
    
    //internal const string SM_SettingsCategory = "Share Bubbles Mod";
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    #endregion Constants & Category
    
    #region Debug Settings

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Inbound", description: "Log inbound Mod Network updates.");

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Outbound", description: "Log outbound Mod Network updates.");

    #endregion Debug Settings

    #region Initialization
    
    internal static void Initialize()
    {
  
    }
    
    #endregion Initialization

    #region Setting Changed Callbacks
    
    // private static void OnPlayerUpAlignmentThresholdChanged(float oldValue, float newValue)
    // {
    //     Entry_PlayerUpAlignmentThreshold.Value = Mathf.Clamp(newValue, 0f, 180f);
    // }

    #endregion Setting Changed Callbacks
}