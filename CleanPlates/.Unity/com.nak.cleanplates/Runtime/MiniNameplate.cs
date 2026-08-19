// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using TMPro;
using UnityEngine;

namespace NAK.CleanPlates.UI
{
    public class MiniNameplate : MonoBehaviour
    {
        [Header("Structure")]
        [SerializeField] private CanvasGroup root;
        [SerializeField] private RectTransform pillRect;
        [SerializeField] private RoundedHexGraphic pill;
        [SerializeField] private CanvasGroup textGroup;
        [SerializeField] private RectTransform nameRect;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private RectTransform iconRoot;

        [Header("Sizing")]
        // Above the content width this pads one end and breaks the even spacing.
        [SerializeField] private float minWidth = 40f;
        [SerializeField] private float maxWidth = 320f;
        [SerializeField] private float iconWidth = 22f;
        [SerializeField] private float edgePadding = 3f;
        [SerializeField] private float iconGap = 10f;
        [SerializeField, Range(0f, 1f)] private float accentOpacity = 0.3f;

        private float targetWidth = 90f;
        private Color pillBaseColor;
        private bool capturedPill;

        // edgePadding is the gap past the cap, not the whole end inset, which keeps the
        // pill clear of its corners on rounder shapes.
        private float EdgeInset => RoundedHexGraphic.InsetFor(pillRect.sizeDelta.y, edgePadding);
        private float IconTextInset => EdgeInset + iconWidth + iconGap;
        // The aspect has to be applied here or a hexagon draws squashed.
        private float FarWidth => pillRect.sizeDelta.y * RoundedHexGraphic.ShapeAspect;

        private void CapturePill()
        {
            if (capturedPill) return;
            capturedPill = true;
            pillBaseColor = pill.color;
        }

        public void SetBackgroundOpacity(float value)
        {
            CapturePill();
            Color color = pillBaseColor;
            color.a *= value;
            pill.color = color;
        }

        public void Bind(string username, Color primary, Color secondary)
        {
            username = SafeText.Clean(username);
            nameText.text = username;

            float insetSum = IconTextInset + EdgeInset;
            nameRect.sizeDelta = new Vector2(-insetSum, nameRect.sizeDelta.y);
            nameRect.anchoredPosition = new Vector2((IconTextInset - EdgeInset) * 0.5f, nameRect.anchoredPosition.y);
            targetWidth = Mathf.Clamp(nameText.GetPreferredValues(username).x + insetSum, minWidth, maxWidth);

            Color left = NameplateView.TameAccent(primary);
            Color right = NameplateView.TameAccent(secondary);
            left.a = accentOpacity;
            right.a = accentOpacity;
            pill.SetAccents(left, right);
        }

        public void SetAlpha(float alpha) => root.alpha = alpha;

        public void SetState(float alpha, float blend)
        {
            root.alpha = alpha;

            float widthT = NameplateView.Fade(blend, 0f, 0.8f);
            float width = Mathf.Lerp(FarWidth, targetWidth, widthT);
            pillRect.sizeDelta = new Vector2(width, pillRect.sizeDelta.y);

            // Lerped on the same curve as the width, or the icon drifts outside the
            // shrinking pill.
            float inset = -(width - iconWidth) * 0.5f + EdgeInset;
            iconRoot.anchoredPosition = new Vector2(
                Mathf.Lerp(0f, inset, widthT),
                iconRoot.anchoredPosition.y);

            textGroup.alpha = NameplateView.Fade(blend, 0.6f, 0.95f);
        }
    }
}