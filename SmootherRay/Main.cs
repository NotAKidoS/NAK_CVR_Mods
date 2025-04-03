using System;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;

namespace NAK.SmootherRay;

// ChilloutVR adaptation of:
// https://github.com/kinsi55/BeatSaber_SmoothedController
// https://github.com/kinsi55/BeatSaber_SmoothedController/blob/master/LICENSE

public class SmootherRayMod : MelonMod
{
    internal static MelonLogger.Instance Logger; 
    
    #region Melon Preferences
    
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(SmootherRayMod));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enable Smoothing", true,
        description: "Enable or disable smoothing.");

    public static readonly MelonPreferences_Entry<bool> EntryMenuOnly =
        Category.CreateEntry("Menu Only", false,
        description: "Only use smoothing on Main Menu and Quick Menu. This will be fine for most users, but it may be desired on pickups & Unity UI elements too. When off it is best paired with WhereAmIPointing.");
    
    public static readonly MelonPreferences_Entry<float> EntryPositionSmoothing =
        Category.CreateEntry("Position Smoothing (3f)", 3f,
        description: "How much to smooth position changes by. Use the slider to adjust the position smoothing factor. Range: 0 to 20.");

    public static readonly MelonPreferences_Entry<float> EntryRotationSmoothing =
        Category.CreateEntry("Rotation Smoothing (12f)", 12f,
        description: "How much to smooth rotation changes by. Use the slider to adjust the rotation smoothing factor. Range: 0 to 20.");

    public static readonly MelonPreferences_Entry<float> EntrySmallMovementThresholdAngle =
        Category.CreateEntry("Small Angle Threshold (6f)", 6f,
        description: "Angle difference to consider a 'small' movement. The less shaky your hands are, the lower you probably want to set this. This is probably the primary value you want to tweak. Use the slider to adjust the threshold angle. Range: 4 to 15.");

    public static readonly MelonPreferences_Entry<bool> EntrySmoothWhenHoldingPickup =
        Category.CreateEntry("Smooth When Holding Pickup", false,
        description: "Enable or disable smoothing when holding a pickup.");
        
    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(PlayerSetup_Patches));
        ApplyPatches(typeof(ControllerSmoothing_Patches));
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
    
    #endregion Melon Events
    
    #region Harmony Patches
    
    internal static class PlayerSetup_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
        private static void Postfix_PlayerSetup_Start(ref PlayerSetup __instance)
        {
            __instance.vrLeftHandTracker.gameObject.AddComponent<SmootherRayer>().ray = __instance.vrRayLeft;
            __instance.vrRightHandTracker.gameObject.AddComponent<SmootherRayer>().ray = __instance.vrRayRight;
        }
    }

    internal static class ControllerSmoothing_Patches
    {
        // SmootherRay
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ControllerSmoothing), nameof(ControllerSmoothing.OnAppliedPoses))]
        private static bool Prefix_ControllerSmoothing_OnAppliedPoses(ref ControllerSmoothing __instance)
            => !EntryEnabled.Value; // SmootherRay method enforces identity local pos when disabled, so we skip it
    }
    
    #endregion Harmony Patches
}