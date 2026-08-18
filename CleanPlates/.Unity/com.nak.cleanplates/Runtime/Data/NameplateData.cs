using UnityEngine;

namespace NAK.CleanPlates.UI
{
    public class NameplateData
    {
        public string Username;
        public string Pronouns;
        public string Status;
        public Texture Icon;
        public bool IsFriend;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Texture GroupBanner;
        public string GroupName;
        public NameplateStatusKind StatusKind = NameplateStatusKind.Online;
        public string RankTag;
        public bool IsNewUser;
        public Color RankColor = new(0.9f, 0.45f, 0.25f);
    }
}