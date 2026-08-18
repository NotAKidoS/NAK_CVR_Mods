// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.CleanPlates.UI
{
    public class NameplateChat : MonoBehaviour
    {
        [Header("Structure")]
        [SerializeField] private CanvasGroup root;
        [SerializeField] private RectTransform bubblesRoot;
        [SerializeField] private ChatBubbleView[] bubbles; // newest first
        [SerializeField] private CanvasGroup typingGroup;
        [SerializeField] private GameObject[] typingDots;
        [SerializeField] private RoundedHexGraphic typingBackground;
        [SerializeField] private CanvasGroup voiceGroup;
        [SerializeField] private RoundedHexGraphic voiceBackground;
        [SerializeField] private GameObject ttsIcon;
        [SerializeField] private GameObject speakerIcon;

        [Header("Colors")]
        [SerializeField] private Color messageColor = new(0.2235294f, 0.7490196f, 0f, 0.75f);
        [SerializeField] private Color oscColor = new(0.2494215f, 0.8962264f, 0.8223274f, 0.75f);
        [SerializeField] private Color modColor = new(0f, 0.6122726f, 0.7490196f, 0.75f);
        [SerializeField, Range(0f, 1f)] private float accentOpacity = 0.3f;

        [Header("Stacking")]
        [SerializeField, Range(0.5f, 1f)] private float stackScale = 0.85f;
        [SerializeField, Range(0f, 1f)] private float stackAlpha = 0.65f;
        [SerializeField] private float bubbleGap = 16f;

        [Header("Typing")]
        [SerializeField] private float typingTimeout = 5f;
        [SerializeField] private float typingDotInterval = 0.5f;
        [SerializeField] private Color typingMutedColor = new(0.55f, 0.55f, 0.6f, 0.5f);
        [SerializeField] private float indicatorGap = 8f;
        [SerializeField, Range(0f, 1f)] private float voiceThreshold = 0.05f;
        [SerializeField] private float voicePulse = 0.25f;
        [SerializeField, Range(0f, 1f)] private float collapsedCutoff = 0.35f;

        private struct Message
        {
            public ChatMessageKind Kind;
            public string SourceLabel;
            public string Text;
            public bool Silent;
            public float ReceivedTime;
            public float Lifetime;
        }

        private readonly List<Message> messages = new();
        private Color accentLeft;
        private Color accentRight;
        private Color typingNormalColor = Color.white;
        private float opacity = 1f;
        private Color typingBaseColor;
        private Color voiceBaseColor;
        private bool capturedBackgrounds;
        private float detail = 1f;
        private float typingUntil;
        private float ttsUntil;
        private float nextDotTime;
        private int dotIndex;
        private float voiceLevel;
        private float appliedVoiceScale = 1f;
        private bool isSpeaking;

        public static bool ShowSpeakerIndicator = true;
        public static bool ShowHistory = true;

        public bool IsTyping { get; private set; }
        public bool IsPlayingTts { get; private set; }
        public bool IsVoiceActive => IsPlayingTts || (ShowSpeakerIndicator && isSpeaking);
        public bool HasMessages => messages.Count > 0;
        // SetDetail already zeroes anything at or under the cutoff.
        public bool IsActive => (IsTyping || IsVoiceActive || messages.Count > 0) && detail > 0f;
        public bool IsVisible => detail > 0f;
        public ChatMessageKind NewestKind => messages.Count > 0 ? messages[0].Kind : ChatMessageKind.Message;

        private Graphic[] typingDotGraphics;
        // One entry per stack position.
        private float[] stackAlphas;
        private float[] stackScales;

        private void Awake()
        {
            typingGroup.gameObject.SetActive(false);
            voiceGroup.gameObject.SetActive(false);
            typingDotGraphics = new Graphic[typingDots.Length];
            for (int i = 0; i < typingDots.Length; i++)
                typingDotGraphics[i] = typingDots[i].GetComponent<Graphic>();
            typingNormalColor = typingDotGraphics[0].color;

            stackAlphas = new float[bubbles.Length];
            stackScales = new float[bubbles.Length];
            for (int i = 0; i < bubbles.Length; i++)
            {
                stackAlphas[i] = Mathf.Pow(stackAlpha, i);
                stackScales[i] = Mathf.Pow(stackScale, i);
            }

            foreach (var bubble in bubbles) bubble.Clear();
        }

        // Typing still shows with the chatbox off, dimmed and silent.
        public void SetTypingMuted(bool muted)
        {
            Color color = muted ? typingMutedColor : typingNormalColor;
            foreach (Graphic dot in typingDotGraphics) dot.color = color;
        }

        public void SetPlayerColors(Color primary, Color secondary)
        {
            accentLeft = NameplateView.TameAccent(primary);
            accentRight = NameplateView.TameAccent(secondary);
            accentLeft.a = accentOpacity;
            accentRight.a = accentOpacity;
            typingBackground.SetAccents(accentLeft, accentRight);
            voiceBackground.SetAccents(accentLeft, accentRight);
        }

        public void SetBubbleScale(float scale) => bubblesRoot.localScale = Vector3.one * scale;

        public void RefreshHistory() => TrimAndRelayout();

        public void PushMessage(ChatMessageKind kind, string message, bool silent, string sourceLabel = null)
        {
            // Osc and mod messages never enter history, only the first bubble.
            messages.RemoveAll(m => m.Kind != ChatMessageKind.Message);

            messages.Insert(0, new Message
            {
                Kind = kind,
                SourceLabel = sourceLabel,
                Text = message,
                Silent = silent,
                ReceivedTime = Time.time,
                Lifetime = ChatTiming.LifetimeFor(message.Length),
            });
            TrimAndRelayout();
        }

        public void SetTyping(bool typing)
        {
            typingUntil = Time.time + typingTimeout;
            if (typing == IsTyping) return;

            IsTyping = typing;
            if (typing) ResetDots();
            RefreshIndicator();
        }
        
        public void SetPlayingTts(float seconds)
        {
            bool playing = seconds > 0f;
            ttsUntil = Time.time + seconds;
            if (playing == IsPlayingTts) return;

            IsPlayingTts = playing;
            RefreshIndicator();
        }

        public void SetVoiceLevel(float level)
        {
            voiceLevel = level;

            bool speaking = level > voiceThreshold;
            if (speaking != isSpeaking)
            {
                isSpeaking = speaking;
                RefreshIndicator();
            }

            if (!IsVoiceActive) return;

            float scale = 1f + voicePulse * Mathf.Clamp01(voiceLevel);
            if (Mathf.Abs(scale - appliedVoiceScale) < 0.01f) return;
            appliedVoiceScale = scale;
            ttsIcon.transform.localScale = Vector3.one * scale;
            speakerIcon.transform.localScale = Vector3.one * scale;
        }

        private void RefreshIndicator()
        {
            bool voice = IsVoiceActive;
            typingGroup.gameObject.SetActive(IsTyping);
            voiceGroup.gameObject.SetActive(voice);

            ttsIcon.SetActive(IsPlayingTts);
            speakerIcon.SetActive(voice && !IsPlayingTts);

            // Typing keeps the middle and voice moves over when both are up.
            RectTransform voiceRect = (RectTransform)voiceGroup.transform;
            RectTransform typingRect = (RectTransform)typingGroup.transform;
            float offset = IsTyping
                ? (typingRect.sizeDelta.x + voiceRect.sizeDelta.x) * 0.5f + indicatorGap
                : 0f;
            voiceRect.anchoredPosition = new Vector2(offset, voiceRect.anchoredPosition.y);
        }

        private void ResetDots()
        {
            nextDotTime = Time.time + typingDotInterval;
            dotIndex = 0;
            for (int i = 0; i < typingDots.Length; i++)
                typingDots[i].SetActive(i == 0);
        }

        public void SetOpacity(float value)
        {
            opacity = value;
            SetRootAlpha();
        }

        // A CanvasGroup alpha write dirties the whole group.
        private float appliedRootAlpha = -1f;

        private void SetRootAlpha()
        {
            float value = opacity * detail;
            if (Mathf.Abs(value - appliedRootAlpha) < 0.002f) return;
            appliedRootAlpha = value;
            root.alpha = value;
        }

        public void SetBackgroundOpacity(float value)
        {
            CaptureBackgrounds();
            Color typing = typingBaseColor;
            Color voice = voiceBaseColor;
            typing.a *= value;
            voice.a *= value;
            typingBackground.color = typing;
            voiceBackground.color = voice;
            foreach (var bubble in bubbles) bubble.SetBackgroundOpacity(value);
        }

        private void CaptureBackgrounds()
        {
            if (capturedBackgrounds) return;
            capturedBackgrounds = true;
            typingBaseColor = typingBackground.color;
            voiceBaseColor = voiceBackground.color;
        }

        // Collapsed plates drop chat entirely, though the events still arrive from
        // anywhere in the instance.
        public void SetDetail(float value)
        {
            detail = value <= collapsedCutoff ? 0f : Mathf.InverseLerp(collapsedCutoff, 1f, value);
            SetRootAlpha();
        }

        public void Tick(float now)
        {
            bool expired = false;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (now - messages[i].ReceivedTime <= messages[i].Lifetime) continue;
                messages.RemoveAt(i);
                expired = true;
            }
            if (expired) TrimAndRelayout();

            for (int i = 0; i < messages.Count; i++)
                bubbles[i].SetAlpha(stackAlphas[i]
                    * ChatTiming.AlphaFor(now - messages[i].ReceivedTime, messages[i].Lifetime));

            if (IsPlayingTts && now >= ttsUntil) SetPlayingTts(0f);

            if (!IsTyping) return;
            if (now >= typingUntil)
            {
                SetTyping(false);
                return;
            }
            if (now >= nextDotTime)
            {
                nextDotTime = now + typingDotInterval;
                typingDots[dotIndex].SetActive(false);
                dotIndex = (dotIndex + 1) % typingDots.Length;
                typingDots[dotIndex].SetActive(true);
            }
        }

        private void TrimAndRelayout()
        {
            int max = ShowHistory ? bubbles.Length : 1;
            if (messages.Count > max) messages.RemoveRange(max, messages.Count - max);

            float prevTop = 0f;
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (i >= messages.Count)
                {
                    bubbles[i].Clear();
                    continue;
                }

                Message m = messages[i];
                bubbles[i].Set(ColorFor(m.Kind), m.Text, m.Silent, m.SourceLabel,
                    i == 0 && m.Kind == ChatMessageKind.Message);

                float scale = stackScales[i];
                var rect = (RectTransform)bubbles[i].transform;
                // Pinned by the bottom edge.
                float half = rect.sizeDelta.y * scale * 0.5f;
                float y = i == 0 ? half : prevTop + bubbleGap + half;
                rect.anchoredPosition = new Vector2(0f, y);
                prevTop = y + half;

                bubbles[i].SetScale(scale);
            }
        }

        private Color ColorFor(ChatMessageKind kind) => kind switch
        {
            ChatMessageKind.OSC => oscColor,
            ChatMessageKind.Mod => modColor,
            _ => messageColor,
        };
    }
}