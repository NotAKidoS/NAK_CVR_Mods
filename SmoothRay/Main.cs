﻿using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;

namespace NAK.SmoothRay;

// ChilloutVR adaptation of:
// https://github.com/kinsi55/BeatSaber_SmoothedController
// https://github.com/kinsi55/BeatSaber_SmoothedController/blob/master/LICENSE

public class SmoothRay : MelonMod
{
    #region Melon Preferences
    
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(SmoothRay));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enable Smoothing", true,
        description: "Enable or disable smoothing.");

    public static readonly MelonPreferences_Entry<bool> EntryMenuOnly =
        Category.CreateEntry("Menu Only", true,
        description: "Only use smoothing on Main Menu and Quick Menu. This will be fine for most users, but it may be desired on pickups & Unity UI elements too.");

    public static readonly MelonPreferences_Entry<float> EntryPositionSmoothing =
        Category.CreateEntry("Position Smoothing", 3f,
        description: "How much to smooth position changes by. Use the slider to adjust the position smoothing factor. Range: 0 to 20.");

    public static readonly MelonPreferences_Entry<float> EntryRotationSmoothing =
        Category.CreateEntry("Rotation Smoothing", 12f,
        description: "How much to smooth rotation changes by. Use the slider to adjust the rotation smoothing factor. Range: 0 to 20.");

    public static readonly MelonPreferences_Entry<float> EntrySmallMovementThresholdAngle =
        Category.CreateEntry("Small Angle Threshold", 6f,
        description: "Angle difference to consider a 'small' movement. The less shaky your hands are, the lower you probably want to set this. This is probably the primary value you want to tweak. Use the slider to adjust the threshold angle. Range: 4 to 15.");

    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(PlayerSetup_Patches));
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
            __instance.vrLeftHandTracker.gameObject.AddComponent<SmoothRayer>().ray = __instance.vrRayLeft;
            __instance.vrRightHandTracker.gameObject.AddComponent<SmoothRayer>().ray = __instance.vrRayRight;
        }
    }
    
    #endregion Harmony Patches
}