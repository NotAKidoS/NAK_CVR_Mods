using MelonLoader;

namespace NAK.LegacyContentMitigation;

internal static class ModSettings
{
    #region Constants

    internal const string ModName = nameof(LegacyContentMitigation);
    internal const string LCM_SettingsCategory = "Legacy Content Mitigation";

    #endregion Constants

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    internal static readonly MelonPreferences_Entry<bool> EntryAutoForLegacyWorlds =
        Category.CreateEntry("auto_for_legacy_worlds", true,
            "Auto For Legacy Worlds", description: "Should Legacy View be auto enabled for detected Legacy worlds?");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorDistance =
        Category.CreateEntry("face_mirror_distance", 0.5f,
            "Face Mirror Distance", description: "Distance from the camera to place the face mirror.");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorOffsetX =
        Category.CreateEntry("face_mirror_offset_x", 0f,
            "Face Mirror Offset X", description: "Offset the face mirror on the X axis.");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorOffsetY =
        Category.CreateEntry("face_mirror_offset_y", 0f,
            "Face Mirror Offset Y", description: "Offset the face mirror on the Y axis.");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorSizeX =
        Category.CreateEntry("face_mirror_size_x", 0.5f,
            "Face Mirror Size X", description: "Size of the face mirror on the X axis.");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorSizeY =
        Category.CreateEntry("face_mirror_size_y", 0.5f,
            "Face Mirror Size Y", description: "Size of the face mirror on the Y axis.");
    
    internal static readonly MelonPreferences_Entry<float> EntryFaceMirrorCameraScale =
        Category.CreateEntry("face_mirror_camera_scale", 1f,
            "Face Mirror Camera Scale", description: "Scale of the face mirror camera.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryUseFaceMirror =
        Category.CreateEntry("use_face_mirror", true,
            "Use Face Mirror", description: "Should the face mirror be used?");
    
    #endregion Melon Preferences
}