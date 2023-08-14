using MelonLoader;

namespace NAK.AvatarScaleMod;

// i like this

internal static class ModSettings
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarScaleMod));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle AvatarScaleMod entirely for Local user. Kinda.");

    public static readonly MelonPreferences_Entry<bool> EntryUseScaleGesture =
        Category.CreateEntry("Scale Gesture", false, description: "Use two fists to scale yourself easily.");

    static ModSettings()
    {
        EntryEnabled.OnEntryValueChanged.Subscribe(OnEntryEnabledChanged);
        EntryUseScaleGesture.OnEntryValueChanged.Subscribe(OnEntryUseScaleGestureChanged);
    }
    
    public static void InitializeModSettings()
    {
        AvatarScaleManager.GlobalEnabled = EntryEnabled.Value;
        AvatarScaleGesture.GestureEnabled = EntryUseScaleGesture.Value;
    }

    private static void OnEntryEnabledChanged(bool oldValue, bool newValue)
    {
        AvatarScaleManager.GlobalEnabled = newValue;
    }

    private static void OnEntryUseScaleGestureChanged(bool oldValue, bool newValue)
    {
        AvatarScaleGesture.GestureEnabled = newValue;
    }
}