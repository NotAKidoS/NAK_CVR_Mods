using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Self;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.Communications;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.NetworkIK;
using ABI_RC.Systems.PlayerColors;
using GameCommon.Models.Player;
using NAK.CleanPlates.Helpers;
using UnityEngine;

namespace NAK.CleanPlates;

public static partial class PlateManager
{
    private struct Frame
    {
        public float DeltaTime;
        public bool Inspecting;
        public bool MenuOpen;
        public bool ShowLocal;
        public bool ShowRemote;
        public bool ShowCamera;
        public Vector3 ObserverPosition;
    }

    private static Frame _frame;
    private static bool _inspecting;
    private static bool _hasRunSetup;

    private static CVRSettings.CVRSettingsOptionMenu _localVisibility;
    private static CVRSettings.CVRSettingsOptionMenu _remoteVisibility;
    private static CVRSettings.CVRSettingsOptionFriends _imageVisibility;
    private static CVRSettings.CVRSettingsOptionMenu _cameraVisibility;

    internal static void Init()
    {
        // The chonk.
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Setup);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(OnPlayerLeaveEntity);
        CVRGameEventSystem.Instance.OnDisconnected.AddListener(OnInstanceDisconnected);
        CVRFrameDriver.OnPostLateUpdate += Tick;
        PlayerColorsManager.OnRemotePlayerColorChanged += OnRemotePlayerColorChanged;
        PlayerColorsManager.OnLocalPlayerColorChanged += OnLocalPlayerColorChanged;
        Friends.OnFriendListUpdated += OnFriendsListUpdated;
        ShowRank.NameplateShowRankChanged += UpdateLocalRank;

        // Game job offsets every overhead by this
        PlayerNameplate.PositionOffset = HeightClearance;

        // ChatBox
        ChatBoxAPI.OnMessageReceived += OnMessageReceived;
        ChatBoxAPI.OnIsTypingReceived += OnIsTypingReceived;
        ChatBoxAPI.OnIsTypingSent += OnIsTypingSent;
    }

    private static void Setup()
    {
        if (_hasRunSetup) return;

        CVRSettings settings = MetaPort.Instance.settings;
        _localVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(PlayerNameplate.NameplateLocalVisibilitySettingName);
        _remoteVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(PlayerNameplate.NameplateRemoteVisibilitySettingName);
        _imageVisibility = (CVRSettings.CVRSettingsOptionFriends)settings.GetSettingsInt(PlayerNameplate.NameplateShowProfileImgSettingName);
        _cameraVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(IndicatorPlate.VisibilitySettingName);
        settings.settingIntChanged.AddListener(OnSettingIntChanged);

        CVRSelfModerationManager moderation = MetaPort.Instance.SelfModerationManager;
        moderation.OnPlayerMuteSet.AddListener(static (userId, _) => RefreshMuted(userId));
        moderation.OnPlayerVolumeSet.AddListener(static (userId, _) => RefreshMuted(userId));

        _hasRunSetup = true;
    }

    private static void OnSettingIntChanged(string settingName, int settingValue)
    {
        switch (settingName)
        {
            case PlayerNameplate.NameplateLocalVisibilitySettingName:
                _localVisibility = (CVRSettings.CVRSettingsOptionMenu)settingValue;
                break;
            case PlayerNameplate.NameplateRemoteVisibilitySettingName:
                _remoteVisibility = (CVRSettings.CVRSettingsOptionMenu)settingValue;
                break;
            case PlayerNameplate.NameplateShowProfileImgSettingName:
                _imageVisibility = (CVRSettings.CVRSettingsOptionFriends)settingValue;
                RefreshAllIcons();
                break;
            case IndicatorPlate.VisibilitySettingName:
                _cameraVisibility = (CVRSettings.CVRSettingsOptionMenu)settingValue;
                break;
            case ChatBoxBubbleBehavior.SettingBubbleOpacityName:
            case ChatBoxBubbleBehavior.SettingBubbleSizeName:
                ForEachChat(ApplyBubbleSettings);
                break;
        }
    }

    private static void OnRemotePlayerColorChanged(PlayerBase player, PlayerColors color)
        => ApplyPlayerColors(player, color);

    private static void OnLocalPlayerColorChanged(PlayerColors color)
        => ApplyPlayerColors(PlayerSetup.Instance, color);

    private static void ApplyPlayerColors(PlayerBase player, PlayerColors color)
    {
        UpdateData(player, d =>
        {
            d.PrimaryColor = color.PrimaryColor;
            d.SecondaryColor = color.SecondaryColor;
        });
        SetPlayerColors(player, color);
    }

    private static void Tick()
    {
        if (!_hasRunSetup) return;

        bool inspecting = IsInspecting();
        if (inspecting != _inspecting)
        {
            _inspecting = inspecting;
            OnInspectingChanged(inspecting);
        }

        _frame.DeltaTime = Time.deltaTime;
        _frame.Inspecting = inspecting;
        _frame.MenuOpen = IsMenuOpen();
        _frame.ShowLocal = ShouldDisplay(_localVisibility);
        _frame.ShowRemote = ShouldDisplay(_remoteVisibility);
        _frame.ShowCamera = ShouldDisplay(_cameraVisibility);
        _frame.ObserverPosition = PlayerSetup.Instance.activeCam.transform.position;

        TickPlates(entries, ref scanIndex, PlateProfiler);
        TickPlates(indicators, ref indicatorScanIndex, CameraProfiler);
    }

    // Indicators skip overhead, chat and voice
    private static void TickPlates(List<Entry> plates, ref int scan, LoopProfiler profiler)
    {
        int count = plates.Count;
        if (count == 0) return;

        float dt = _frame.DeltaTime;
        bool menuOpen = _frame.MenuOpen;
        bool inspecting = _frame.Inspecting;

        int checks = Mathf.Min(count,
            Mathf.Max(DistanceChecksPerFrame, count / DistanceCheckDivisor));
        for (int i = 0; i < checks; i++)
        {
            scan = (scan + 1) % count;
            Entry e = plates[scan];
            if (e.Anchor == null) continue;
            e.Distance = Vector3.Distance(_frame.ObserverPosition, e.Anchor.position);
            if (!e.IsIndicator && !e.IsLocal) e.InNearBand = IsInNearBand(e);
        }

        profiler.Begin();
        int processed = 0;

        float now = Time.time;
        float scaleLerp = 1f - Mathf.Exp(-ScaleSmoothing * dt);
        float fovScale = FovScale.Current;

        foreach (Entry e in plates)
        {
            if (e.Anchor == null) continue;

            bool indicator = e.IsIndicator;
            bool show = indicator ? _frame.ShowCamera : e.IsLocal ? _frame.ShowLocal : _frame.ShowRemote;

            // Own indicators sit in front of us, own nameplate does not
            e.NearHidden = NearHide && (indicator || !e.IsLocal) && e.Distance <= NearHideDistance
                                         + (e.NearHidden ? NearHideHysteresis : 0f);

            bool visible = show && (menuOpen || inspecting || !e.NearHidden)
                                && ((e.InNearBand && e.Distance <= HideDistance)
                                    || (inspecting && e.Distance <= MaxRevealDistance));
            bool full = e.Distance < FullDetailDistance;

            float targetAlpha = visible ? Opacity : 0f;
            float targetBlend = full ? 1f : 0f;
            float targetDetail = inspecting && !indicator ? 1f : 0f;

            if (inspecting && now < e.RevealTime)
            {
                targetAlpha = e.Alpha;
                targetBlend = e.LodBlend;
                targetDetail = e.DetailBlend;
            }

            float alpha = Mathf.MoveTowards(e.Alpha, targetAlpha, FadeSpeed * dt);
            float blend = Mathf.MoveTowards(e.LodBlend, targetBlend, LodBlendSpeed * dt);
            float detail = Mathf.MoveTowards(e.DetailBlend, targetDetail, LodBlendSpeed * dt);

            // Resize rebuilds mesh (expensive) so alpha and scale are separate dirty flags

            if (alpha != e.Alpha) e.AlphaDirty = true;
            if (blend != e.LodBlend || detail != e.DetailBlend) e.Dirty = true;
            e.Alpha = alpha;
            e.LodBlend = blend;
            e.DetailBlend = detail;

            if (!indicator && !TickOverhead(e, now, dt))
            {
                e.Scale = -1f; // snap on the way back in instead of sliding
                continue;
            }

            if (e.Dirty)
            {
                e.View.SetState(e.Alpha, e.LodBlend, e.DetailBlend);
                e.Dirty = false;
                e.AlphaDirty = false;
            }
            else if (e.AlphaDirty)
            {
                e.View.SetAlpha(e.Alpha);
                e.AlphaDirty = false;
            }

            processed++;

            float targetScale = indicator
                ? Mathf.Clamp(e.Distance * fovScale / FullDetailDistance,
                      1f, inspecting ? CameraMaxScale : CameraCollapsedMaxScale)
                  * Mathf.Lerp(CameraFarScale, CameraNearScale, blend)
                : Mathf.Clamp(e.Distance * fovScale / (inspecting ? InspectReadableDistance : ReadableDistance),
                      1f, inspecting ? MaxScale : CollapsedMaxScale)
                  / e.Plate.NameSizeAtLod(blend) * UserScale;

            float scale = e.Scale < 0f
                ? targetScale
                : Mathf.Lerp(e.Scale, targetScale, scaleLerp);
            if (Mathf.Abs(scale - e.Scale) > 0.001f)
            {
                e.Scale = scale;
                e.Anchor.localScale = e.BaseScale * scale;
            }
        }

        profiler.End(processed, count);
    }

    // Collapse, voice, chat, then overhead active
    private static bool TickOverhead(Entry e, float now, float dt)
    {
        bool collapsed = e.LodBlend < CollapseThreshold;
        if (collapsed != e.Collapsed)
        {
            e.Collapsed = collapsed;
            ThirdpartySupport.RaisePlateCollapsedChanged(e.Player, collapsed);
        }

        float talkTarget = Mathf.Clamp01(e.GetCommsSmoothAmplitude());
        float talkSpeed = talkTarget > e.TalkLevel ? TalkAttack : TalkRelease;
        e.TalkLevel = Mathf.MoveTowards(e.TalkLevel, talkTarget, talkSpeed * dt);

        string userId = e.Player.PlayerId;
        bool packetSpeaking = e.TalkLevel < TalkStep && VoiceActivity.IsSpeaking(userId);
        e.Chat.SetVoiceLevel(packetSpeaking ? PacketSpeakingLevel : e.TalkLevel);
        if (e.IsLocal ? Comms_Manager.IsTTSPlaying : VoiceActivity.IsPlayingTts(userId))
            e.Chat.SetPlayingTts(TtsHoldSeconds);
        e.Chat.SetDetail(e.LodBlend);
        e.Chat.Tick(now);

        OverheadController controller = e.OverheadController;
        controller.UpdateActiveState();
        if (!controller.IsActive) return false;

        if (Mathf.Abs(e.TalkLevel - e.AppliedTalk) > TalkStep
            || (e.TalkLevel == talkTarget && e.TalkLevel != e.AppliedTalk))
        {
            e.AppliedTalk = e.TalkLevel;
            e.Plate.SetTalk(e.TalkLevel); // tints the body graphic, so this is a rebuild
        }

        return true;
    }

    private static bool IsInNearBand(Entry e) => e.Puppet == null || e.Puppet.IsNameplateVisible;

    private static bool ShouldDisplay(CVRSettings.CVRSettingsOptionMenu visibility)
        => AuthManager.IsAuthenticated
           && MetaPort.Instance.worldEnableNameplates
           && (visibility == CVRSettings.CVRSettingsOptionMenu.Always
               || (visibility == CVRSettings.CVRSettingsOptionMenu.MenuOpened && _frame.MenuOpen));

    // Polled for if nameplates are set to only display during menu open.
    private static bool IsMenuOpen()
        => ViewManager.Instance.IsAnyMenuOpen
           || CVRInputManager.Instance.unlockMouse;

    // Plates become big and readable size no matter distance.
    private static bool IsInspecting()
        => MetaPort.Instance.isUsingVr
            ? CVR_MenuManager.Instance.IsViewShown
            : CVRInputManager.Instance.unlockMouse;
}