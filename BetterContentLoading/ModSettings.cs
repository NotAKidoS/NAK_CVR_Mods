using MelonLoader;

namespace NAK.RCCVirtualSteeringWheel;

internal static class ModSettings
{
    #region Constants

    private const string ModName = nameof(RCCVirtualSteeringWheel);

    #endregion Constants

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    internal static readonly MelonPreferences_Entry<bool> EntryOverrideSteeringRange =
        Category.CreateEntry("override_steering_range", false,
            "Override Steering Range", description: "Should the steering wheel use a custom steering range instead of the vehicle's default?");
            
    internal static readonly MelonPreferences_Entry<float> EntryCustomSteeringRange =
        Category.CreateEntry("custom_steering_range", 60f,
            "Custom Steering Range", description: "The custom steering range in degrees when override is enabled (default: 60)");
            
    internal static readonly MelonPreferences_Entry<bool> EntryInvertSteering =
        Category.CreateEntry("invert_steering", false,
            "Invert Steering", description: "Inverts the steering direction");

    #endregion Melon Preferences
}