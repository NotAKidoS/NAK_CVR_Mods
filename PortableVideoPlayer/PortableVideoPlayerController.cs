using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.VideoPlayer.Scripts;
using ABI.CCK.Components;
using NAK.PortableVideoPlayer.UI;
using UnityEngine;

namespace NAK.PortableVideoPlayer;

public class PortableVideoPlayerController : MonoBehaviour
{
    internal static PortableVideoPlayerController Instance { get; private set; }

    internal PickupUiElement Pickup { get; private set; }
    internal ViewManagerVideoPlayer LinkedUi { get; private set; }

    private PortableVideoPlayerView _view;
    private CVRVideoPlayer _linkedPlayer;

    private bool _isPlaying;
    private float _volume = -1f;
    private Texture _screenTexture;

    #region Unity Events

    private void Awake()
    {
        Instance = this;

        _view = GetComponent<PortableVideoPlayerView>();
        _view.LinkClicked += CycleNextVideoPlayer;
        _view.PlayPauseClicked += OnPlayPauseClicked;
        _view.MuteClicked += OnMuteClicked;
        _view.VolumeChanged += OnVolumeChanged;
        _view.ProgressChanged += OnProgressChanged;
        _view.PickupAllowedChanged += OnPickupAllowedChanged;
        _view.HoverChanged += OnHoverChanged;

        Pickup = gameObject.AddComponent<PickupUiElement>();
        Pickup._maximumPushDistance = 1f;

        _checkInactivity = CheckPointerOverlayInactivity;
        CVRGameEventSystem.World.OnWorldUnload.AddListener(OnWorldUnload);

        Unlink();

        // Apply initial scale
        ApplyCalculatedTargetScale();
    }

    private void Start()
    {
        CycleNextVideoPlayer();
    }

    private void Update()
    {
        CheckForScaleChange();
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
            Instance = null;

        CVRGameEventSystem.World.OnWorldUnload.RemoveListener(OnWorldUnload);
        StopInactivityJob();
    }

    #endregion Unity Events

    #region Linking

    private void OnWorldUnload(string _, CVRObjectLoader.WorldLoadIntent __) => Unlink();

    private static bool TryGetUi(CVRVideoPlayer player, out ViewManagerVideoPlayer ui)
    {
        ui = null;
        if (!player || !player.interactiveUI || !player.gameObject.activeInHierarchy)
            return false;

        if (player.VideoPlayerUis.Count == 0)
            return false;

        ui = player.VideoPlayerUis[0] as ViewManagerVideoPlayer;
        return ui;
    }

    private void CycleNextVideoPlayer()
    {
        CVRWorld world = CVRWorld.Instance;
        if (!world)
            return;

        List<CVRVideoPlayer> allPlayers = world._registeredVideoPlayers;
        int count = allPlayers.Count;
        // Get the index of the currently connected video player
        int currentIndex = _linkedPlayer ? allPlayers.IndexOf(_linkedPlayer) : -1;

        // Find next active video player
        for (int i = 1; i <= count; i++)
        {
            CVRVideoPlayer player = allPlayers[(currentIndex + i + count) % count];
            // Skip if there is no interactive ui we can connect to
            if (!TryGetUi(player, out ViewManagerVideoPlayer ui))
                continue;

            // Connect to the next video player
            Link(player, ui);
            return;
        }

        Unlink();
    }

    private void Link(CVRVideoPlayer player, ViewManagerVideoPlayer ui)
    {
        _linkedPlayer = player;
        LinkedUi = ui;

        _view.SetLinkedPlayerName(player.gameObject.name);
        _view.ClearTime();
        _view.SetProgress(ui.videoProgressSlider.value, ui.videoProgressSlider.maxValue);

        _volume = -1f;

        // Do a full sync of the UI state
        SyncState();
    }

    private void Unlink()
    {
        _linkedPlayer = null;
        LinkedUi = null;

        _isPlaying = false;
        _volume = -1f;
        _screenTexture = null;

        _view.SetLinkedPlayerName(null);
        _view.SetVideoInfo(string.Empty, string.Empty);
        _view.SetPlaying(false);
        _view.SetScreenTexture(null);
        _view.ClearTime();
    }

    #endregion Linking

    #region Video Player Events

    internal void SyncState()
    {
        ViewManagerVideoPlayer ui = LinkedUi;
        CVRVideoPlayer player = _linkedPlayer;

        _view.SetVideoInfo(ui.videoName.text, ui.videoUrl.text);

        bool isPlaying = ui.buttonPause.activeSelf;
        if (isPlaying != _isPlaying)
        {
            _isPlaying = isPlaying;
            _view.SetPlaying(isPlaying);
        }

        float volume = player.playbackVolume;
        if (volume != _volume)
        {
            _volume = volume;
            _view.SetVolume(volume);
        }

        Texture screenTexture = player.ProjectionTexture;
        if (!ReferenceEquals(screenTexture, _screenTexture))
        {
            _screenTexture = screenTexture;
            _view.SetScreenTexture(screenTexture);
        }
    }

    internal void OnTimeUpdated(double videoTime, double duration, bool isLiveStream)
        => _view.SetTime(videoTime, duration, isLiveStream);

    #endregion Video Player Events

    #region Ui Handlers

    private void OnPlayPauseClicked()
    {
        if (!LinkedUi) return;

        if (_isPlaying)
            LinkedUi.Pause();
        else
            LinkedUi.Play();
    }

    private void OnMuteClicked()
    {
        if (!LinkedUi) return;

        LinkedUi.AudioMuteToggle();
        SyncState();
    }

    private void OnVolumeChanged(float value)
    {
        if (!LinkedUi) return;

        LinkedUi.videoAudioSlider.value = value;
        SyncState();
    }

    private void OnProgressChanged(float value)
    {
        if (!LinkedUi) return;

        LinkedUi.videoProgressSlider.value = value;
    }

    private void OnPickupAllowedChanged(bool allowed)
        => Pickup.IsPickupable = allowed;

    #endregion Ui Handlers

    #region Activity

    private Action _checkInactivity;
    private ScheduledJob _overlayFadeUpdateJob;
    private bool _showOverlay;
    private bool _fadeOutInactivity;

    private void OnHoverChanged(bool hovered)
    {
        _showOverlay = hovered;
        _fadeOutInactivity = false;
        _view.SetOverlayVisible(hovered);

        if (hovered)
            // Check for inactivity
            _overlayFadeUpdateJob ??= BetterScheduleSystem.AddJob(_checkInactivity, 0.25f, 0.3f, -1);
        else
            // Stop checking for inactivity
            StopInactivityJob();
    }

    private void StopInactivityJob()
    {
        if (_overlayFadeUpdateJob == null) return;
        BetterScheduleSystem.RemoveJob(_overlayFadeUpdateJob);
        _overlayFadeUpdateJob = null;
    }

    private void CheckPointerOverlayInactivity()
    {
        if (MetaPort.Instance.isUsingVr) return;

        CVRInputManager inputManager = CVRInputManager.Instance;
        double totalSeconds = DateTime.UtcNow.Subtract(inputManager.lastInputChangeUtc).TotalSeconds;
        if (inputManager.unlockMouse) totalSeconds = 0;

        if (_showOverlay && !_fadeOutInactivity && totalSeconds > 2)
        {
            _showOverlay = false;
            _fadeOutInactivity = true;
            _view.SetOverlayVisible(false);
        }
        else if (!_showOverlay && _fadeOutInactivity && totalSeconds < 2)
        {
            _showOverlay = true;
            _fadeOutInactivity = false;
            _view.SetOverlayVisible(true);
        }
    }

    #endregion Activity

    #region Pickup Handling

    private const float ScaleSpeed = 1.5f;

    private float _scaleModifier = 0.8f; // 1.5f to 0.5f

    private void ApplyCalculatedTargetScale()
    {
        transform.localScale = Vector3.one * (PlayerSetup.Instance.GetPlaySpaceScale() * _scaleModifier);
    }

    private void CheckForScaleChange()
    {
        if (!Pickup.IsPickedUp)
            return;

        if (!MetaPort.Instance.isUsingVr)
            return;

        CVRInputManager inputManager = CVRInputManager.Instance;

        float leftRightInput;
        if (Pickup.ControllerRay.hand == CVRHand.Left)
        {
            if (inputManager.interactLeftValue < 0.5f) return;
            leftRightInput = inputManager.movementVector.x;
            inputManager.movementVector = Vector3.zero; // Consume input
        }
        else
        {
            if (inputManager.interactRightValue < 0.5f) return;
            leftRightInput = inputManager.rawLookVector.x;
            inputManager.rawLookVector = Vector2.zero; // Consume input
        }

        if (Mathf.Abs(leftRightInput) < 0.1f)
            return; // No significant input

        _scaleModifier = Mathf.Clamp(_scaleModifier + leftRightInput * ScaleSpeed * Time.deltaTime, 0.1f, 1.25f);
        ApplyCalculatedTargetScale();
    }

    #endregion Pickup Handling

    #region Static

    internal static bool Toggle()
    {
        if (Instance)
        {
            Destroy(Instance.gameObject);
            Instance = null;
            return false;
        }

        PlayerSetup playerSetup = PlayerSetup.Instance;
        Transform playerTransform = playerSetup.transform;
        Transform playerCamTransform = playerSetup.activeCam.transform;
        Vector3 transForward = playerCamTransform.forward;

        Vector3 position = playerCamTransform.position + transForward * playerSetup.GetPlaySpaceScale();
        Quaternion rotation = Quaternion.LookRotation(transForward, playerTransform.up);

        Instantiate(PortableVideoPlayerMod.PortableVideoPlayerPrefab, position, rotation, playerTransform)
            .AddComponent<PortableVideoPlayerController>();

        // Apply config
        SetOverrenderWorld(PortableVideoPlayerMod.EntryOverrenderWorld.Value);
        return true;
    }

    internal static void SetOverrenderWorld(bool overrender)
    {
        if (!Instance) return;
        Instance.gameObject.SetLayerRecursive(overrender ? CVRLayers.UIInternal : CVRLayers.UI);
    }

    #endregion Static
}
