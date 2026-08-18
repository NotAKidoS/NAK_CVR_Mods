using UnityEngine;

namespace NAK.CleanPlates.UI
{
    public class NameplateDemo : MonoBehaviour
    {
        [SerializeField] private Nameplate plate;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float backgroundOpacity = 1f;
        [SerializeField] private bool showIconSlot = true;

        [Header("Data")]
        [SerializeField] private string username = "KJoy";
        [SerializeField] private string pronouns = "they/them";
        [SerializeField] private string status = "Just hanging out";
        [SerializeField] private NameplateStatusKind statusKind = NameplateStatusKind.Online;
        [SerializeField] private bool isFriend = true;
        [SerializeField] private Texture icon;
        [SerializeField] private Color primaryColor = new(0.3f, 0.6f, 1f);
        [SerializeField] private Color secondaryColor = new(1f, 0.3f, 0.8f);
        [SerializeField] private Texture groupBanner;
        [SerializeField] private string rankTag = "";
        [SerializeField] private bool isNewUser;
        [SerializeField] private Color rankColor = new(0.9f, 0.45f, 0.25f);

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float alpha = 1f;
        [SerializeField, Range(0f, 1f)] private float lodBlend = 1f;
        [SerializeField, Range(0f, 1f)] private float detailBlend;
        [SerializeField, Range(0f, 1f)] private float talkLevel;

        private void Start() => Rebind();

        private void OnValidate()
        {
            if (Application.isPlaying && plate != null) Rebind();
        }

        private void Update()
        {
            if (plate == null) return;
            plate.SetState(alpha, lodBlend, detailBlend);
            plate.SetTalk(talkLevel);
        }

        private void Rebind()
        {
            if (plate == null) return;

            NameplateView.BackgroundOpacity = backgroundOpacity;
            NameplateView.ShowIconSlot = showIconSlot;
            plate.Bind(new NameplateData
            {
                Username = username,
                Pronouns = pronouns,
                Status = status,
                StatusKind = statusKind,
                Icon = icon,
                IsFriend = isFriend,
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                GroupBanner = groupBanner,
                RankTag = rankTag,
                IsNewUser = isNewUser,
                RankColor = rankColor,
            });
        }
    }
}