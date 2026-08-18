// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.CleanPlates.UI
{
    // Full plate, the shared layout plus the status row, status dot and group banner.
    public class Nameplate : NameplateView
    {
        [Header("Full plate")]
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private RectTransform statusRect;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image statusDot;
        [SerializeField] private RectTransform statusDotRect;
        [SerializeField] private RoundedHexGraphic groupBanner;
        [SerializeField]
        private Color[] statusColors =
        {
            new(0f, 0f, 0f, 0f),          // None
            new(0.35f, 0.85f, 0.45f, 1f), // Online
            new(0.95f, 0.6f, 0.2f, 1f),   // Away
            new(0.9f, 0.3f, 0.3f, 1f),    // Busy
            new(0.55f, 0.6f, 0.65f, 1f),  // Offline
        };
        [SerializeField] private float iconTextInset = 114f;
        [SerializeField] private float textInset = 39f;
        [SerializeField] private float bannerMaxWidth = 300f;
        [SerializeField] private float bannerHeight = 28f;
        [SerializeField] private float bannerHeightExpanded = 76f;
        [SerializeField] private Vector2 pronounOffset = new(70f, -70f);
        [SerializeField] private float nameRowY = 10f;

        private bool hasGroupBanner;

        protected override string SubtitleFor(NameplateData data)
            => SafeText.Clean(data.Pronouns);

        protected override float Layout(NameplateData data, string username)
        {
            bool status = !string.IsNullOrEmpty(data.Status);
            statusText.gameObject.SetActive(status);
            statusText.text = SafeText.Clean(data.Status);
            statusDot.gameObject.SetActive(status && data.StatusKind != NameplateStatusKind.None);

            pronounPillRect.anchoredPosition = pronounOffset;

            hasGroupBanner = data.GroupBanner != null;
            bannerGroup.gameObject.SetActive(hasGroupBanner);
            if (hasGroupBanner) groupBanner.Texture = data.GroupBanner;

            // The rect spans the content area right of the icon.
            float insetSum = iconTextInset + textInset;
            float shift = (iconTextInset - textInset) * 0.5f;
            nameRect.sizeDelta = new Vector2(-insetSum, nameRect.sizeDelta.y);
            nameRect.anchoredPosition = new Vector2(shift, status ? nameRowY : 0f);
            statusRect.sizeDelta = new Vector2(-insetSum, statusRect.sizeDelta.y);
            statusRect.anchoredPosition = new Vector2(shift, statusRect.anchoredPosition.y);

            float needed = nameText.GetPreferredValues(username).x;
            if (status)
            {
                float statusW = statusText.GetPreferredValues(statusText.text).x;
                needed = Mathf.Max(needed, statusW);
                statusDot.color = statusColors[(int)data.StatusKind];
                statusDotRect.anchoredPosition = new Vector2(
                    shift - statusW * 0.5f - 14f, statusDotRect.anchoredPosition.y);
            }

            return Mathf.Clamp(needed + insetSum + 10f, minWidth, maxWidth);
        }

        protected override void StateExtras(float width, float detailBlend, float bodyAlpha, float textAlpha)
        {
            if (!hasGroupBanner) return;

            bannerGroup.alpha = textAlpha;

            // Fixed width clamped to the straight top run.
            float capWidth = bodyRect.sizeDelta.y * RoundedHexGraphic.CapInset;
            bannerRect.sizeDelta = new Vector2(
                Mathf.Min(bannerMaxWidth, width - 2f * capWidth + 6f),
                Mathf.Lerp(bannerHeight, bannerHeightExpanded, Fade(detailBlend, 0f, 1f)));
        }
    }
}