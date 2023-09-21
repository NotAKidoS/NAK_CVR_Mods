using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;

namespace NAK.AvatarScaleMod;

// i like this

internal static class ModSettings
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarScaleMod));

    public static MelonPreferences_Entry<bool> PersistantHeight;
    
    // AvatarScaleTool supported scaling settings
    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("AvatarScaleTool Scaling", true, description: "Should there be persistant avatar scaling? This only works properly across supported avatars.");
    
    // Universal scaling settings (Mod Network, requires others to have mod)
    public static readonly MelonPreferences_Entry<bool> EntryUniversalScaling =
        Category.CreateEntry("Force Universal Scaling", false, description: "Should the mod use Mod Network for scaling? This makes it work on all avatars, but others need the mod.");
    public static readonly MelonPreferences_Entry<bool> EntryScaleConstraints =
        Category.CreateEntry("Scale Constraints", false, description: "Should constraints be scaled with Universal Scaling?");
    public static readonly MelonPreferences_Entry<bool> EntryScaleLights =
        Category.CreateEntry("Scale Lights", false, description: "Should lights be scaled with Universal Scaling?");
    public static readonly MelonPreferences_Entry<bool> EntryScaleAudioSources =
        Category.CreateEntry("Scale Audio Sources", false, description: "Should audio sources be scaled with Universal Scaling?");
    
    // General scaling settings
    public static readonly MelonPreferences_Entry<bool> EntryUseScaleGesture =
        Category.CreateEntry("Scale Gesture", false, description: "Use two fists to scale yourself easily.");
    
    // Internal settings
    public static readonly MelonPreferences_Entry<float> HiddenLastAvatarScale =
        Category.CreateEntry("Last Avatar Scale", -1f, is_hidden: true);
    
    static ModSettings()
    {
        EntryEnabled.OnEntryValueChanged.Subscribe(OnEntryEnabledChanged);
        EntryUseScaleGesture.OnEntryValueChanged.Subscribe(OnEntryUseScaleGestureChanged);
        EntryUniversalScaling.OnEntryValueChanged.Subscribe(OnEntryUniversalScalingChanged);
        EntryScaleConstraints.OnEntryValueChanged.Subscribe(OnEntryScaleConstraintsChanged);
    }

    private static void OnEntryEnabledChanged(bool oldValue, bool newValue)
    {
        //AvatarScaleManager.UseUniversalScaling = newValue;
    }

    private static void OnEntryUseScaleGestureChanged(bool oldValue, bool newValue)
    {
        //AvatarScaleGesture.GestureEnabled = newValue;
    }



    private static void OnEntryUniversalScalingChanged(bool oldValue, bool newValue)
    {

    }

    private static void OnEntryScaleConstraintsChanged(bool oldValue, bool newValue)
    {

    }
    
    public static void InitializeModSettings()
    {
        PersistantHeight = Category.CreateEntry("Persistant Height", false, description: "Should the avatar height persist between avatar switches?");
        PersistantHeight.OnEntryValueChanged.Subscribe(OnPersistantHeightChanged);
        
        //AvatarScaleManager.UseUniversalScaling = EntryEnabled.Value;
        //AvatarScaleGesture.GestureEnabled = EntryUseScaleGesture.Value;
    }
    
    private static void OnPersistantHeightChanged(bool oldValue, bool newValue)
    {
        AvatarScaleManager.Instance.Setting_PersistantHeight = newValue;
    }
}