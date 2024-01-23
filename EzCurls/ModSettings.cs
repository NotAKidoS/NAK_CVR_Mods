using MelonLoader;
using UnityEngine;

namespace NAK.EzCurls;

public static class ModSettings
{
    #region Melon Prefs

    private const string SettingsCategory = nameof(EzCurls);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    // Tuned to the values Liquid uses, sign language nerd
    
    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true,
            description: "Enable EzCurls.");

    private static readonly MelonPreferences_Entry<bool> EntryUseCurlSnapping =
        Category.CreateEntry("UseCurlSnapping", true, 
            description: "Finger curl snapping to a specified value when in the specified range.");

    private static readonly MelonPreferences_Entry<float> EntrySnappedCurlValue =
        Category.CreateEntry("SnappedCurlValue", 0.4f,
            description: "The value to which the finger curl snaps within the range.");

    private static readonly MelonPreferences_Entry<float> EntryRangeStartPercent =
        Category.CreateEntry("RangeStartPercent", 0.25f,
            description: "The minimum value for the SnappedCurlValue range.");

    private static readonly MelonPreferences_Entry<float> EntryRangeEndPercent =
        Category.CreateEntry("RangeEndPercent", 0.5f,
            description: "The maximum value for the SnappedCurlValue range.");

    private static readonly MelonPreferences_Entry<bool> EntryUseCurlSmoothing =
        Category.CreateEntry("UseCurlSmoothing", true,
            description: "Finger curl smoothing to average out similar finger positions.");

    private static readonly MelonPreferences_Entry<bool> EntryOnlySmoothNearbyCurl =
        Category.CreateEntry("OnlySmoothNearbyCurl", false,
            description: "Should the curl smoothing only influence the nearest curl?");
    
    private static readonly MelonPreferences_Entry<bool> EntryDontSmoothExtremes =
        Category.CreateEntry("DontSmoothExtremes", true,
            description: "Should the finger curl smoothing be less effective on curls towards 0 or 1?");

    private static readonly MelonPreferences_Entry<float> EntryCurlSimilarityThreshold =
        Category.CreateEntry("CurlSimilarityThreshold", 0.5f,
            description: "The threshold for curl similarity during curl smoothing.");

    private static readonly MelonPreferences_Entry<float> EntryCurlSmoothingFactor =
        Category.CreateEntry("CurlSmoothingFactor", 0.4f,
            description: "The multiplier for curl smoothing.");
    
    // Curve control settings
    private static readonly MelonPreferences_Entry<bool> EntryUseCurveControl =
        Category.CreateEntry("UseCurveControl", true,
            description: "Enable curve control mode to make the midrange of the curl more dense.");
    
    private static readonly MelonPreferences_Entry<bool> EntryUseTunedCurveControl =
        Category.CreateEntry("UseTunedCurveControl", true,
            description: "Enables a pre-tuned curve.");

    private static readonly MelonPreferences_Entry<float> EntryCurveMin =
        Category.CreateEntry("CurveMin", 0.0f,
            description: "The minimum value of the density curve.");

    private static readonly MelonPreferences_Entry<float> EntryCurveMiddle =
        Category.CreateEntry("CurveMiddle", 0.5f,
            description: "The middle value of the density curve.");

    private static readonly MelonPreferences_Entry<float> EntryCurveMax =
        Category.CreateEntry("CurveMax", 1.0f,
            description: "The maximum value of the density curve.");

    #endregion

    internal static void Initialize()
    {
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }
    
    internal static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        if (InputModuleCurlAdjuster.Instance == null)
            return;
        
        // enabled
        InputModuleCurlAdjuster.Instance.enabled = EntryEnabled.Value;

        // curl snapping
        InputModuleCurlAdjuster.Instance.UseCurlSnapping = EntryUseCurlSnapping.Value;
        InputModuleCurlAdjuster.Instance.SnappedCurlValue = EntrySnappedCurlValue.Value;
        InputModuleCurlAdjuster.Instance.RangeStartPercent = EntryRangeStartPercent.Value;
        InputModuleCurlAdjuster.Instance.RangeEndPercent = EntryRangeEndPercent.Value;

        // curl smoothing
        InputModuleCurlAdjuster.Instance.UseCurlSmoothing = EntryUseCurlSmoothing.Value;
        InputModuleCurlAdjuster.Instance.OnlySmoothNearbyCurl = EntryOnlySmoothNearbyCurl.Value;
        InputModuleCurlAdjuster.Instance.DontSmoothExtremes = EntryDontSmoothExtremes.Value;
        InputModuleCurlAdjuster.Instance.CurlSimilarityThreshold = EntryCurlSimilarityThreshold.Value;
        InputModuleCurlAdjuster.Instance.CurlSmoothingFactor = EntryCurlSmoothingFactor.Value;
        
        // curve control
        InputModuleCurlAdjuster.Instance.UseCurveControl = EntryUseCurveControl.Value;
        InputModuleCurlAdjuster.Instance.UseTunedCurveControl = EntryUseTunedCurveControl.Value;
        InputModuleCurlAdjuster.Instance.DensityCurve = new AnimationCurve(
            new Keyframe(0, EntryCurveMin.Value),
            new Keyframe(0.5f, EntryCurveMiddle.Value),
            new Keyframe(1, EntryCurveMax.Value)
        );
    }
}