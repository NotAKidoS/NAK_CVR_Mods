using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK.SubSystems;

namespace NAK.EzCurls;

internal class InputModuleCurlAdjuster : CVRInputModule
{
    public static InputModuleCurlAdjuster Instance;

    // Curl clamping/adjustment
    public bool UseCurlSnapping = false;
    public float SnappedCurlValue = 0.5f;
    public float RangeStartPercent = 0.5f;
    public float RangeEndPercent = 0.8f;

    // Curl smoothing/averaging
    public bool UseCurlSmoothing = false;
    public bool DontSmoothExtremes = true;
    public bool OnlySmoothNearbyCurl = false;
    public float CurlSimilarityThreshold = 0.5f;
    public float CurlSmoothingFactor = 0.5f;

    public new void Start()
    {
        Instance = this;
        base.Start();
        EzCurls.OnSettingsChanged();
    }

    public override void UpdateInput() => DoCurlAdjustments();
    public override void UpdateImportantInput() => DoCurlAdjustments();

    private void DoCurlAdjustments()
    {
        if (!_inputManager.individualFingerTracking)
            return;

        if (UseCurlSmoothing)
        {
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

        if (UseCurlSnapping)
        {
            SnapCurls(ref _inputManager.fingerCurlLeftIndex);
            SnapCurls(ref _inputManager.fingerCurlLeftMiddle);
            SnapCurls(ref _inputManager.fingerCurlLeftRing);
            SnapCurls(ref _inputManager.fingerCurlLeftPinky);

            SnapCurls(ref _inputManager.fingerCurlRightIndex);
            SnapCurls(ref _inputManager.fingerCurlRightMiddle);
            SnapCurls(ref _inputManager.fingerCurlRightRing);
            SnapCurls(ref _inputManager.fingerCurlRightPinky);
        }
    }

    private void SnapCurls(ref float fingerCurl)
    {
        if (fingerCurl >= RangeStartPercent && fingerCurl <= RangeEndPercent)
            fingerCurl = SnappedCurlValue;
    }

    private void SmoothCurls(ref float index, ref float middle, ref float ring, ref float pinky)
    {
        List<float> values = new List<float> { index, middle, ring, pinky };

        for (int i = 0; i < values.Count; i++)
        {
            for (int j = i + 1; j < values.Count; j++)
            {
                if (Math.Abs(values[i] - values[j]) <= CurlSimilarityThreshold)
                {
                    // Compute new smoothed values
                    float smoothedValue = (values[i] + values[j]) / 2;

                    // Calculate smoothing factors for both values
                    float smoothingFactor1 = CalculateSmoothingFactor(values[i]);
                    float smoothingFactor2 = CalculateSmoothingFactor(values[j]);

                    // Adjust both values towards the smoothed value
                    values[i] = values[i] + smoothingFactor1 * CurlSmoothingFactor * (smoothedValue - values[i]);
                    values[j] = values[j] + smoothingFactor2 * CurlSmoothingFactor * (smoothedValue - values[j]);
                }
            }
        }

        index = values[0];
        middle = values[1];
        ring = values[2];
        pinky = values[3];
    }

    private void SmoothCurlsNear(ref float index, ref float middle, ref float ring, ref float pinky)
    {
        List<float> values = new List<float> { index, middle, ring, pinky };

        for (int i = 0; i < values.Count - 1; i++)
        {
            if (Math.Abs(values[i] - values[i + 1]) <= CurlSimilarityThreshold)
            {
                // Compute new smoothed value
                float smoothedValue = (values[i] + values[i + 1]) / 2;

                // Calculate smoothing factors for both values
                float smoothingFactor1 = CalculateSmoothingFactor(values[i]);
                float smoothingFactor2 = CalculateSmoothingFactor(values[i + 1]);

                // Adjust both values towards the smoothed value
                values[i] = values[i] + smoothingFactor1 * CurlSmoothingFactor * (smoothedValue - values[i]);
                values[i + 1] = values[i + 1] + smoothingFactor2 * CurlSmoothingFactor * (smoothedValue - values[i + 1]);
            }
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
        float dist = curlValue - 0.5f;
        return 1.0f - 4 * dist * dist;
    }
}