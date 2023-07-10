using ABI_RC.Core.Savior;
using MelonLoader;
using System.Reflection;

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

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(InputModuleSteamVR).GetMethod(nameof(InputModuleSteamVR.Start)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(EzCurls).GetMethod(nameof(OnInputModuleSteamVRStart_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
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
    }

    private static void OnInputModuleSteamVRStart_Prefix(ref InputModuleSteamVR __instance)
    {
        __instance.gameObject.AddComponent<InputModuleCurlAdjuster>();
    }
}