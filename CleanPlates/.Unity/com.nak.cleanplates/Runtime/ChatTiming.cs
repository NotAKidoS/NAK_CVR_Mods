// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using UnityEngine;

namespace NAK.CleanPlates.UI
{
    public static class ChatTiming
    {
        // "College-educated adults typically read between 200 and 300 WPM,
        // while reading aloud drops to an average of 183 WPM."

        public const float MinLifetime = 5f;
        public const float MaxLifetime = 20f;
        public const float SecondsPerChar = 60f / (180f * 5f); // 180 wpm at ~5 chars per word
        public const float FadeFraction = 0.2f; // last fraction of lifetime spent fading out

        public static float LifetimeFor(int messageLength)
            => Mathf.Min(MinLifetime + messageLength * SecondsPerChar, MaxLifetime);

        public static float AlphaFor(float age, float lifetime)
        {
            float fadeStart = lifetime * (1f - FadeFraction);
            return age < fadeStart
                ? 1f
                : Mathf.Clamp01(1f - (age - fadeStart) / (lifetime * FadeFraction));
        }
    }
}