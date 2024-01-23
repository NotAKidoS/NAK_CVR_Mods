using ABI_RC.Systems.InputManagement;
using UnityEngine;

namespace NAK.EzCurls;

internal class InputModuleCurlAdjuster : CVRInputModule
{
    public static InputModuleCurlAdjuster Instance { get; private set; }

    #region Variables

    public bool enabled = false;
    
    // Curl clamping/adjustment
    public bool UseCurlSnapping = false;
    public float SnappedCurlValue = 0.4f;
    public float RangeStartPercent = 0.25f;
    public float RangeEndPercent = 0.5f;

    // Curl smoothing/averaging
    public bool UseCurlSmoothing = true;
    public bool DontSmoothExtremes = true;
    public bool OnlySmoothNearbyCurl = false;
    public float CurlSimilarityThreshold = 0.5f;
    public float CurlSmoothingFactor = 0.4f;

    // Curve control
    public bool UseCurveControl = false;
    public AnimationCurve DensityCurve = new(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    // Tuned Curve control
    public bool UseTunedCurveControl = false;

    private readonly AnimationCurve TunedDensityCurve = new(
        new Keyframe(0, 0), // Start at (0, 0)
        new Keyframe(0.5f, 0.5f), // Normal behavior up to (0.3, 0.3)
        new Keyframe(0.85f, 0.6f), // Only 0.1f movement from 0.5 to 0.9
        new Keyframe(1, 1) // Normal behavior from 0.9 to 1
    );
    
    #endregion

    #region Overrides

    public override void ModuleAdded()
    {
        if (Instance != null
            && Instance != this)
            return;

        Instance = this;

        base.ModuleAdded();
        ModSettings.OnSettingsChanged();
    }

    public override void UpdateInput()
        => DoCurlAdjustments();

    #endregion
    
    private void DoCurlAdjustments()
    {
        if (!enabled
            || !_inputManager.individualFingerTracking)
            return;

        DoCurlSmoothing();
        DoCurlSnapping();
        DoCurlCurveControl();
    }

    #region Smooth Curls

    private void DoCurlSmoothing()
    {
        if (!UseCurlSmoothing)
            return;

        if (OnlySmoothNearbyCurl)
        {
            SmoothCurlsNear(
                ref _inputManager.fingerCurlLeftIndex,
                ref _inputManager.fingerCurlLeftMiddle,
                ref _inputManager.fingerCurlLeftRing,
                ref _inputManager.fingerCurlLeftPinky
            );

            SmoothCurlsNear(
                ref _inputManager.fingerCurlRightIndex,
                ref _inputManager.fingerCurlRightMiddle,
                ref _inputManager.fingerCurlRightRing,
                ref _inputManager.fingerCurlRightPinky
            );
        }
        else
        {
            SmoothCurls(
                ref _inputManager.fingerCurlLeftIndex,
                ref _inputManager.fingerCurlLeftMiddle,
                ref _inputManager.fingerCurlLeftRing,
                ref _inputManager.fingerCurlLeftPinky
            );

            SmoothCurls(
                ref _inputManager.fingerCurlRightIndex,
                ref _inputManager.fingerCurlRightMiddle,
                ref _inputManager.fingerCurlRightRing,
                ref _inputManager.fingerCurlRightPinky
            );
        }
    }

    private void SmoothCurls(ref float index, ref float middle, ref float ring, ref float pinky)
    {
        var values = new List<float> { index, middle, ring, pinky };

        for (var i = 0; i < values.Count; i++)
        for (var j = i + 1; j < values.Count; j++)
            if (Math.Abs(values[i] - values[j]) <= CurlSimilarityThreshold)
            {
                // Compute new smoothed values
                var smoothedValue = (values[i] + values[j]) / 2;

                // Calculate smoothing factors for both values
                var smoothingFactor1 = CalculateSmoothingFactor(values[i]);
                var smoothingFactor2 = CalculateSmoothingFactor(values[j]);

                // Adjust both values towards the smoothed value
                values[i] = values[i] + smoothingFactor1 * CurlSmoothingFactor * (smoothedValue - values[i]);
                values[j] = values[j] + smoothingFactor2 * CurlSmoothingFactor * (smoothedValue - values[j]);
            }

        index = values[0];
        middle = values[1];
        ring = values[2];
        pinky = values[3];
    }

    private void SmoothCurlsNear(ref float index, ref float middle, ref float ring, ref float pinky)
    {
        var values = new List<float> { index, middle, ring, pinky };

        for (var i = 0; i < values.Count - 1; i++)
            if (Math.Abs(values[i] - values[i + 1]) <= CurlSimilarityThreshold)
            {
                // Compute new smoothed value
                var smoothedValue = (values[i] + values[i + 1]) / 2;

                // Calculate smoothing factors for both values
                var smoothingFactor1 = CalculateSmoothingFactor(values[i]);
                var smoothingFactor2 = CalculateSmoothingFactor(values[i + 1]);

                // Adjust both values towards the smoothed value
                values[i] = values[i] + smoothingFactor1 * CurlSmoothingFactor * (smoothedValue - values[i]);
                values[i + 1] = values[i + 1] +
                                smoothingFactor2 * CurlSmoothingFactor * (smoothedValue - values[i + 1]);
            }

        index = values[0];
        middle = values[1];
        ring = values[2];
        pinky = values[3];
    }

    // calculate the smoothing factor based on the curl value
    private float CalculateSmoothingFactor(float curlValue)
    {
        if (!DontSmoothExtremes)
            return 1f;

        // Compute the distance from the center (0.5) and square it
        var dist = curlValue - 0.5f;
        return 1.0f - 4 * dist * dist;
    }

    #endregion

    #region Snap Curls

    private void DoCurlSnapping()
    {
        if (!UseCurlSnapping)
            return;

        SnapCurls(ref _inputManager.fingerCurlLeftIndex);
        SnapCurls(ref _inputManager.fingerCurlLeftMiddle);
        SnapCurls(ref _inputManager.fingerCurlLeftRing);
        SnapCurls(ref _inputManager.fingerCurlLeftPinky);

        SnapCurls(ref _inputManager.fingerCurlRightIndex);
        SnapCurls(ref _inputManager.fingerCurlRightMiddle);
        SnapCurls(ref _inputManager.fingerCurlRightRing);
        SnapCurls(ref _inputManager.fingerCurlRightPinky);
    }

    private void SnapCurls(ref float fingerCurl)
    {
        if (fingerCurl >= RangeStartPercent && fingerCurl <= RangeEndPercent)
            fingerCurl = SnappedCurlValue;
    }

    #endregion

    #region Curve Curls

    private void DoCurlCurveControl()
    {
        if (!UseCurveControl)
            return;

        AdjustCurlUsingCurve(ref _inputManager.fingerCurlLeftIndex);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlLeftMiddle);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlLeftRing);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlLeftPinky);

        AdjustCurlUsingCurve(ref _inputManager.fingerCurlRightIndex);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlRightMiddle);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlRightRing);
        AdjustCurlUsingCurve(ref _inputManager.fingerCurlRightPinky);
    }

    // middle of curve is more "dense"
    private void AdjustCurlUsingCurve(ref float fingerCurl)
    {
        if (UseTunedCurveControl)
        {
            fingerCurl = TunedDensityCurve.Evaluate(fingerCurl);
            return;
        }

        fingerCurl = DensityCurve.Evaluate(fingerCurl);
    }

    #endregion
}