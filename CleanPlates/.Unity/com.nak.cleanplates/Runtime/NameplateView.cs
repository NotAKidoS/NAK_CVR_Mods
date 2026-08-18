// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.CleanPlates.UI
{
    // Shared by both plate styles, and the prefabs reference these serialized names by
    // hand.
    public abstract class NameplateView : MonoBehaviour
    {
        [Header("Structure")]
        [SerializeField] protected CanvasGroup root;
        [SerializeField] protected RectTransform bodyRect;
        [SerializeField] protected CanvasGroup bodyGroup;
        [SerializeField] protected CanvasGroup textGroup;
        [SerializeField] protected CanvasGroup pronounGroup;
        [SerializeField] protected CanvasGroup farNameGroup;
        [SerializeField] protected RectTransform iconRoot;
        [SerializeField] protected RectTransform pronounPillRect;
        [SerializeField] protected RectTransform farPillRect;
        [SerializeField] protected RectTransform nameRect;
        [SerializeField] protected RectTransform[] cornerAnchors;
        [SerializeField] protected GameObject newUserFlair;
        [SerializeField] protected NameplateChat chat;

        [Header("Content")]
        [SerializeField] protected TMP_Text nameText;
        [SerializeField] protected TMP_Text pronounsText;
        [SerializeField] protected TMP_Text farNameText;
        [SerializeField] protected RoundedHexGraphic icon;
        [SerializeField] protected GameObject iconImage;
        [SerializeField] protected RoundedHexGraphic monogramBg;
        [SerializeField] protected TMP_Text monogramText;
        [SerializeField] protected RoundedHexGraphic bodyGraphic;
        [SerializeField] protected RoundedHexGraphic pronounPillGraphic;
        [SerializeField] protected RoundedHexGraphic farPillGraphic;
        [SerializeField] protected RectTransform rankBadgeRect;
        [SerializeField] protected Graphic rankBadgeBg;
        [SerializeField] protected TMP_Text rankTagText;

        [Header("Sizing")]
        [SerializeField] protected float minWidth = 320f;
        [SerializeField] protected float maxWidth = 700f;
        [SerializeField] protected float pronounPillPadding = 4f;
        [SerializeField] protected float rankBadgePadding = 6f;
        [SerializeField] protected Color talkTint = new(0.42f, 0.5f, 0.62f, 0.92f);
        [SerializeField, Range(0f, 1f)] protected float accentOpacity = 0.3f;
        // Rim graphics skip the background fade.
        [SerializeField] protected RoundedHexGraphic[] accentOutlineGraphics;
        [SerializeField, Range(0f, 1f)] protected float outlineAccentOpacity = 0.85f;
        // Floor for the rim, which follows the opacity setting but never reaches zero.
        [SerializeField, Range(0f, 1f)] protected float outlineMinOpacity;

        [Header("Baked, do not edit by hand")]
        [SerializeField] protected float bottomExtent;
        [SerializeField] protected Color bodyBaseColor;
        [SerializeField] protected Graphic[] backgroundGraphics;
        [SerializeField] protected Color[] backgroundBaseColors;

        protected static readonly Color FriendColor = new(1f, 0.85f, 0.35f);

        public NameplateChat Chat => chat;
        // SetState is the only writer and always runs before a tick reads this.
        private float bodyWidth;
        public float BodyWidth => bodyWidth;
        public float BodyHeight => bodyRect.sizeDelta.y;
        // How far the plate reaches below its origin, which the body height alone does
        // not give.
        public float BottomExtent => bottomExtent;

        // Collapsing swaps the username for the smaller far text.
        public float NameSizeAtLod(float lodBlend)
            => Mathf.Lerp(farNameText.fontSize, nameText.fontSize, lodBlend) / nameText.fontSize;

        // Corners arrive from the third party api as a loose int cast to the enum.
        public bool TryGetCornerAnchor(NameplateCorner corner, out RectTransform anchor)
        {
            int index = (int)corner;
            anchor = (uint)index < (uint)cornerAnchors.Length ? cornerAnchors[index] : null;
            return anchor != null;
        }

        public static float BackgroundOpacity = 1f;
        // Whether the style has an image slot at all, not whether a given player's
        // image may be shown.
        public static bool ShowIconSlot = true;

        protected float targetWidth = 320f;

        public void Bind(NameplateData data)
        {
            for (int i = 0; i < backgroundGraphics.Length; i++)
            {
                Color background = backgroundBaseColors[i];
                background.a *= BackgroundOpacity;
                backgroundGraphics[i].color = background;
            }

            string username = SafeText.Clean(data.Username);
            nameText.text = username;
            farNameText.text = username;

            Color nameColor = data.IsFriend ? FriendColor : Color.white;
            nameText.color = nameColor;
            farNameText.color = nameColor;

            BindIcon(data, username);
            BindFlairs(data);
            BindSubtitle(SubtitleFor(data));
            BindAccents(data);

            float farW = farNameText.GetPreferredValues(username).x
                         + RoundedHexGraphic.InsetFor(farPillRect.sizeDelta.y, pronounPillPadding) * 2f;
            farPillRect.sizeDelta = new Vector2(farW, farPillRect.sizeDelta.y);

            targetWidth = Layout(data, username);
        }

        private void BindIcon(NameplateData data, string username)
        {
            iconRoot.gameObject.SetActive(ShowIconSlot);
            if (!ShowIconSlot) return;

            bool ugc = data.Icon != null;
            iconImage.SetActive(ugc);
            monogramBg.gameObject.SetActive(!ugc);
            monogramText.gameObject.SetActive(!ugc);
            icon.Texture = data.Icon;
            if (ugc) return;

            Color mono = TameAccent(data.PrimaryColor);
            mono.a = 1f;
            monogramBg.color = mono;
            monogramText.text = string.IsNullOrEmpty(username)
                ? "?"
                : username.Substring(0, Mathf.Min(3, username.Length)).ToUpperInvariant();
        }

        private void BindFlairs(NameplateData data)
        {
            newUserFlair.SetActive(data.IsNewUser);

            bool rank = !string.IsNullOrEmpty(data.RankTag);
            rankBadgeRect.gameObject.SetActive(rank);
            if (!rank) return;

            Color rc = TameAccent(data.RankColor);
            rc.a = 1f;
            rankBadgeBg.color = rc;
            rankTagText.text = SafeText.Clean(data.RankTag);
            rankBadgeRect.sizeDelta = new Vector2(
                rankTagText.GetPreferredValues(rankTagText.text).x
                + RoundedHexGraphic.InsetFor(rankBadgeRect.sizeDelta.y, rankBadgePadding) * 2f,
                rankBadgeRect.sizeDelta.y);
        }

        private void BindSubtitle(string subtitle)
        {
            bool has = !string.IsNullOrEmpty(subtitle);
            pronounGroup.gameObject.SetActive(has);
            pronounsText.text = subtitle;
            if (!has) return;

            pronounPillRect.sizeDelta = new Vector2(
                pronounsText.GetPreferredValues(subtitle).x
                + RoundedHexGraphic.InsetFor(pronounPillRect.sizeDelta.y, pronounPillPadding) * 2f,
                pronounPillRect.sizeDelta.y);
        }

        private void BindAccents(NameplateData data)
        {
            Color left = TameAccent(data.PrimaryColor);
            Color right = TameAccent(data.SecondaryColor);
            left.a = accentOpacity;
            right.a = accentOpacity;
            bodyGraphic.SetAccents(left, right);
            pronounPillGraphic.SetAccents(left, right);
            farPillGraphic.SetAccents(left, right);

            // Set here and not in the background pass, which has no floor to apply.
            float rimOpacity = Mathf.Max(BackgroundOpacity, outlineMinOpacity);
            Color rimBase = bodyBaseColor;
            rimBase.a *= rimOpacity;
            left.a = outlineAccentOpacity * rimOpacity;
            right.a = outlineAccentOpacity * rimOpacity;
            foreach (RoundedHexGraphic rim in accentOutlineGraphics)
            {
                rim.color = rimBase;
                rim.SetAccents(left, right);
            }
        }

        // Alpha alone never changes geometry, and SetState would redo every rect
        // resize.
        public void SetAlpha(float alpha) => root.alpha = alpha;

        public void SetState(float alpha, float lodBlend, float detailBlend)
        {
            root.alpha = alpha;

            float widthT = Fade(lodBlend, 0f, 0.8f);
            // With an image the body collapses onto the icon, whose rect is already the
            // right width.
            float iconSpan = iconRoot.sizeDelta.x;
            float width = Mathf.Lerp(ShowIconSlot ? iconSpan : minWidth, targetWidth, widthT);
            bodyWidth = width;
            bodyRect.sizeDelta = new Vector2(width, bodyRect.sizeDelta.y);

            // Left edge while expanded, centered once collapsed.
            if (ShowIconSlot)
                iconRoot.anchoredPosition = new Vector2(-(width - iconSpan) * 0.5f, iconRoot.anchoredPosition.y);

            float bodyAlpha = ShowIconSlot ? 1f : Fade(lodBlend, 0.05f, 0.4f);
            float textAlpha = Fade(lodBlend, 0.6f, 0.95f);
            float farAlpha = 1f - Fade(lodBlend, 0.2f, 0.55f);
            bodyGroup.alpha = bodyAlpha;
            textGroup.alpha = textAlpha;
            pronounGroup.alpha = textAlpha;
            farNameGroup.alpha = farAlpha;

            StateExtras(width, detailBlend, bodyAlpha, textAlpha);

            bool bodyVisible = ShowIconSlot || bodyAlpha > 0.001f;
            bool farVisible = farAlpha > 0.001f;
            if (bodyGroup.gameObject.activeSelf != bodyVisible) bodyGroup.gameObject.SetActive(bodyVisible);
            if (farNameGroup.gameObject.activeSelf != farVisible) farNameGroup.gameObject.SetActive(farVisible);
        }

        public void SetTalk(float level)
        {
            Color body = Color.Lerp(bodyBaseColor, talkTint, level);
            body.a *= BackgroundOpacity;
            bodyGraphic.color = body;
        }

        protected abstract string SubtitleFor(NameplateData data);
        protected abstract float Layout(NameplateData data, string username);
        protected virtual void StateExtras(float width, float detailBlend, float bodyAlpha, float textAlpha) { }

        internal static float Fade(float t, float from, float to)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t));

        internal static Color TameAccent(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Min(s, 0.6f), Mathf.Clamp(v, 0.5f, 0.85f));
        }
    }
}