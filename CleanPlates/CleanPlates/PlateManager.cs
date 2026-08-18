using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Self;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.PlayerColors;
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
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoaded);
        CVRGameEventSystem.Avatar.OnLocalAvatarClear.AddListener(OnLocalAvatarClear);
        CVRGameEventSystem.Avatar.OnLocalAvatarHeightScale.AddListener(OnLocalAvatarHeightScale);
        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener(OnRemoteAvatarLoaded);
        CVRGameEventSystem.Avatar.OnRemoteAvatarClear.AddListener(OnRemoteAvatarClear);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(OnPlayerLeaveEntity);
        CVRGameEventSystem.Instance.OnDisconnected.AddListener(OnInstanceDisconnected);
        LateEventsManager.OnPostLateUpdate += Tick;
        PlayerColorsManager.OnRemotePlayerColorChanged += OnRemotePlayerColorChanged;
        PlayerColorsManager.OnLocalPlayerColorChanged += OnLocalPlayerColorChanged;
        Friends.OnFriendListUpdated += OnFriendsListUpdated;
        ShowRank.NameplateShowRankChanged += UpdateLocalRank;
        
        // ChatBox
        ChatBoxAPI.AddReceivingInterceptor(MutateChatBoxMessageForTheGreaterGood);
        ChatBoxAPI.OnMessageReceived += OnMessageReceived;
        ChatBoxAPI.OnIsTypingReceived += OnIsTypingReceived;
        ChatBoxAPI.OnMessageSent += OnMessageSent;
        ChatBoxAPI.OnIsTypingSent += OnIsTypingSent;
        CVRGameEventSystem.Communications.TextToSpeech.OnAudioStarted.AddListener(OnTTSAudioStarted);
    }

    private static void Setup()
    {
        if (_hasRunSetup) return;

        // Run nameplate height processing on the blocked prefab in cache!
        RootLogic.Instance.StartExternCoroutine(NameplateAnchorUtility.BakeRoutine(MetaPort.Instance.blockedAvatarPrefab));

        CVRSettings settings = MetaPort.Instance.settings;
        _localVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(PlayerNameplate.NameplateLocalVisibilitySettingName);
        _remoteVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(PlayerNameplate.NameplateRemoteVisibilitySettingName);
        _imageVisibility = (CVRSettings.CVRSettingsOptionFriends)settings.GetSettingsInt(PlayerNameplate.NameplateShowProfileImgSettingName);
        _cameraVisibility = (CVRSettings.CVRSettingsOptionMenu)settings.GetSettingsInt(CameraVisibilitySettingName);
        HeightMode = (NameplateHeightMode)settings.GetSettingsInt(HeightAdjustmentSettingName);
        settings.settingIntChanged.AddListener(OnSettingIntChanged);

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
            case HeightAdjustmentSettingName:
                HeightMode = (NameplateHeightMode)settingValue;
                break;
            case CameraVisibilitySettingName:
                _cameraVisibility = (CVRSettings.CVRSettingsOptionMenu)settingValue;
                break;
            case ChatBoxBubbleBehavior.SettingBubbleOpacityName:
            case ChatBoxBubbleBehavior.SettingBubbleSizeName:
                ForEachChat(ApplyBubbleSettings);
                break;
        }
    }

    private static void OnRemotePlayerColorChanged(string userId, PlayerColors color)
    {
        if (CVRPlayerManager.Instance.TryGetPlayerBase(userId, out PlayerBase player))
            ApplyPlayerColors(player, color);
    }

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

        TickNameplates();
        TickCameraIndicators();
    }

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