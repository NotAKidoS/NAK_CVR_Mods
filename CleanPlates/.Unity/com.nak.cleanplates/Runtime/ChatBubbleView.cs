// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword
// ReSharper disable once ArrangeNamespaceBody

using TMPro;
using UnityEngine;

namespace NAK.CleanPlates.UI
{
    public class ChatBubbleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RoundedHexGraphic outline;
        [SerializeField] private RoundedHexGraphic bg;
        [SerializeField] private TMP_Text text;
        [SerializeField] private GameObject silentFlair;
        [SerializeField] private RoundedHexGraphic silentFlairBg;
        [SerializeField] private RectTransform sourceFlairRect;
        [SerializeField] private RoundedHexGraphic sourceFlairBg;
        [SerializeField] private TMP_Text sourceFlairText;

        [Header("Sizing")]
        [SerializeField] private float minWidth = 90f;
        [SerializeField] private float maxWidth = 460f;
        [SerializeField] private float minHeight = 56f;
        [SerializeField] private float maxHeight = 180f;
        [SerializeField] private float edgePadding = 12f;
        [SerializeField] private float capPaddingScale = 1.2f;
        [SerializeField] private float paddingY = 10f;
        [SerializeField] private float baseFontSize = 30f;
        [SerializeField] private float minFontSize = 18f;
        [SerializeField] private float fontStep = 4f;
        [SerializeField] private float sourceFlairPadding = 6f;
        [SerializeField] private float pointDepth = 6f;
        // Dims the plate behind the text, always under the outline alpha.
        [SerializeField, Range(0f, 1f)] private float innerOpacity = 0.6f;
        // Off, the fill uses fillColor and only the outline and flair show the kind.
        [SerializeField] private bool fillTracksOutline = true;
        [SerializeField] private Color fillColor = new(0.245f, 0.281f, 0.34f, 0.894f);

        private Color outlineColor = Color.white;
        private float opacity = 1f;

        public void Set(Color color, string message, bool silent, string sourceLabel, bool pointed)
        {
            message = SafeText.Clean(message);
            outlineColor = color;
            silentFlair.SetActive(silent);

            // Only the newest bubble gets a tail, and never for mod or osc lines.
            float point = pointed ? pointDepth : 0f;
            bg.SetBottomPoint(point);
            outline.SetBottomPoint(point);

            bool flair = !string.IsNullOrEmpty(sourceLabel);
            sourceFlairRect.gameObject.SetActive(flair);

            ApplyOpacity();

            // Width grows to maxWidth before the font starts shrinking.
            float wrapWidth = maxWidth - PadFor(maxHeight);
            Vector2 preferred = Vector2.zero;

            // Ellipsis reports a collapsed preferred width for anything long enough to
            // truncate.
            text.overflowMode = TextOverflowModes.Overflow;
            for (float size = baseFontSize; size >= minFontSize; size -= fontStep)
            {
                text.fontSize = size;
                preferred = text.GetPreferredValues(message, wrapWidth, 0f);
                if (preferred.y + paddingY <= maxHeight) break;
            }
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (preferred.x <= 1f) preferred.x = wrapWidth;
            text.text = message;
            float height = Mathf.Clamp(preferred.y + paddingY, minHeight, maxHeight);
            float pad = PadFor(height);
            ((RectTransform)transform).sizeDelta = new Vector2(
                Mathf.Clamp(preferred.x + pad, minWidth, maxWidth), height);
            text.rectTransform.sizeDelta = new Vector2(-pad, -paddingY);

            if (flair)
            {
                sourceFlairText.text = sourceLabel;
                float flairH = sourceFlairRect.sizeDelta.y;
                float flairW = sourceFlairText.GetPreferredValues(sourceLabel).x
                               + RoundedHexGraphic.InsetFor(flairH, sourceFlairPadding) * 2f;
                sourceFlairRect.sizeDelta = new Vector2(flairW, flairH);
            }

            gameObject.SetActive(true);
        }

        private float PadFor(float height)
            => height * bg.ActiveCapInset * capPaddingScale + edgePadding;

        public void SetBackgroundOpacity(float value)
        {
            opacity = value;
            ApplyOpacity();
        }

        private void ApplyOpacity()
        {
            // Two translucent layers would compound where they overlap and no stencil
            // is available.
            Color solid = outlineColor;
            solid.a = 1f;
            silentFlairBg.color = solid;
            sourceFlairBg.color = solid;

            Color tint = outlineColor;
            tint.a *= opacity;
            outline.color = tint;

            if (fillTracksOutline)
            {
                tint.a *= innerOpacity;
                bg.color = tint;
                return;
            }

            Color fill = fillColor;
            fill.a *= opacity;
            bg.color = fill;
        }

        public void SetScale(float scale) => transform.localScale = new Vector3(scale, scale, scale);

        // A CanvasGroup alpha write dirties the whole group.
        private float appliedAlpha = -1f;

        public void SetAlpha(float alpha)
        {
            if (Mathf.Abs(alpha - appliedAlpha) < 0.002f) return;
            appliedAlpha = alpha;
            group.alpha = alpha;
        }

        public void Clear() => gameObject.SetActive(false);
    }
}