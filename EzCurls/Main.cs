using ABI_RC.Core.Savior;
using MelonLoader;
using System.Reflection;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.InputManagement.InputModules;
using UnityEngine;

namespace NAK.EzCurls;

public class EzCurls : MelonMod
{
    internal const string SettingsCategory = nameof(EzCurls);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryUseCurlSnapping =
        Category.CreateEntry("UseCurlSnapping", true, 
            description: "Finger curl snapping to a specified value when in the specified range.");

    public static readonly MelonPreferences_Entry<float> EntrySnappedCurlValue =
        Category.CreateEntry("SnappedCurlValue", 0.5f,
            description: "The value to which the finger curl snaps within the range.");

    public static readonly MelonPreferences_Entry<float> EntryRangeStartPercent =
        Category.CreateEntry("RangeStartPercent", 0.5f,
            description: "The minimum value for the SnappedCurlValue range.");

    public static readonly MelonPreferences_Entry<float> EntryRangeEndPercent =
        Category.CreateEntry("RangeEndPercent", 0.8f,
            description: "The maximum value for the SnappedCurlValue range.");

    public static readonly MelonPreferences_Entry<bool> EntryUseCurlSmoothing =
        Category.CreateEntry("UseCurlSmoothing", false,
            description: "Finger curl smoothing to average out similar finger positions.");

    public static readonly MelonPreferences_Entry<bool> EntryOnlySmoothNearbyCurl =
        Category.CreateEntry("OnlySmoothNearbyCurl", false,
            description: "Should the curl smoothing only influence the nearest curl?");


    public static readonly MelonPreferences_Entry<bool> EntryDontSmoothExtremes =
        Category.CreateEntry("DontSmoothExtremes", true,
            description: "Should the finger curl smoothing be less effective on curls towards 0 or 1?");

    public static readonly MelonPreferences_Entry<float> EntryCurlSimilarityThreshold =
        Category.CreateEntry("CurlSimilarityThreshold", 0.1f,
            description: "The threshold for curl similarity during curl smoothing.");

    public static readonly MelonPreferences_Entry<float> EntryCurlSmoothingFactor =
        Category.CreateEntry("CurlSmoothingFactor", 0.5f,
            description: "The multiplier for curl smoothing.");
    
    // Curve control settings
    public static readonly MelonPreferences_Entry<bool> EntryUseCurveControl =
        Category.CreateEntry("UseCurveControl", false,
            description: "Enable curve control mode to make the midrange of the curl more dense.");
    
    public static readonly MelonPreferences_Entry<bool> EntryUseTunedCurveControl =
        Category.CreateEntry("UseTunedCurveControl", false,
            description: "Enables a pre-tuned curve.");

    public static readonly MelonPreferences_Entry<float> EntryCurveMin =
        Category.CreateEntry("CurveMin", 0.0f,
            description: "The minimum value of the density curve.");

    public static readonly MelonPreferences_Entry<float> EntryCurveMiddle =
        Category.CreateEntry("CurveMiddle", 0.5f,
            description: "The middle value of the density curve.");

    public static readonly MelonPreferences_Entry<float> EntryCurveMax =
        Category.CreateEntry("CurveMax", 1.0f,
            description: "The maximum value of the density curve.");

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(CVRInputModule_XR).GetMethod(nameof(CVRInputModule_XR.ModuleAdded)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(EzCurls).GetMethod(nameof(OnCVRInputModule_XRModuleAdded_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }

    public static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        if (InputModuleCurlAdjuster.Instance == null)
            return;

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

    private static void OnCVRInputModule_XRModuleAdded_Postfix()
    {
        CVRInputManager.Instance.AddInputModule(new InputModuleCurlAdjuster());
    }
}