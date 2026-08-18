// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using UnityEngine;

namespace NAK.CleanPlates.UI
{
    // Compact plate, one pill around the username with an optional image left of it.
    public class SimpleNameplate : NameplateView
    {
        [Header("Compact plate")]
        [SerializeField] private float edgePadding = 4f;
        [SerializeField] private float iconGap = 6f;
        // The flairs sit outside Body to draw over the icon.
        [SerializeField] private RectTransform flairsRect;
        [SerializeField] private CanvasGroup flairsGroup;

        private float EdgeInset => RoundedHexGraphic.InsetFor(bodyRect.sizeDelta.y, edgePadding);
        
        protected override string SubtitleFor(NameplateData data)
            => SafeText.Clean(!string.IsNullOrEmpty(data.Status) ? data.Status : data.Pronouns);

        protected override float Layout(NameplateData data, string username)
        {
            // ShapeAspectFitter drives the icon width.
            float leftInset = ShowIconSlot ? iconRoot.sizeDelta.x + iconGap : EdgeInset;
            float insetSum = leftInset + EdgeInset;
            nameRect.sizeDelta = new Vector2(-insetSum, nameRect.sizeDelta.y);
            nameRect.anchoredPosition = new Vector2(
                (leftInset - EdgeInset) * 0.5f, nameRect.anchoredPosition.y);

            return Mathf.Clamp(nameText.GetPreferredValues(username).x + insetSum, minWidth, maxWidth);
        }

        protected override void StateExtras(float width, float detailBlend, float bodyAlpha, float textAlpha)
        {
            flairsRect.sizeDelta = new Vector2(width, flairsRect.sizeDelta.y);
            flairsGroup.alpha = bodyAlpha;

            bool visible = ShowIconSlot || bodyAlpha > 0.001f;
            if (flairsGroup.gameObject.activeSelf != visible) flairsGroup.gameObject.SetActive(visible);
        }
    }
}