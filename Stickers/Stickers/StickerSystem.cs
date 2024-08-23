using System.Diagnostics;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using MTJobSystem;
using NAK.Stickers.Integrations;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers;

public class StickerSystem
{
    #region Singleton

    public static StickerSystem Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
            return;
        
        Instance = new StickerSystem();
        
        // ensure cache folder exists
        if (!Directory.Exists(s_StickersSourcePath)) Directory.CreateDirectory(s_StickersSourcePath);
        
        // configure Decalery
        ModSettings.DecaleryMode selectedMode = ModSettings.Decalery_DecalMode.Value;
        DecalManager.SetPreferredMode((DecalUtils.Mode)selectedMode, selectedMode == ModSettings.DecaleryMode.GPUIndirect, 0);
        if (selectedMode != ModSettings.DecaleryMode.GPU) StickerMod.Logger.Warning("Decalery is not set to GPU mode. Expect compatibility issues with user generated content when mesh data is not marked as readable.");
        
        // listen for game events
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Instance.OnPlayerSetupStart);
    }
    
    #endregion Singleton

    #region Actions

    public static Action<string> OnImageLoadFailed;

    private void InvokeOnImageLoadFailed(string errorMessage)
    {
        MTJobManager.RunOnMainThread("StickersSystem.InvokeOnImageLoadFailed", () => OnImageLoadFailed?.Invoke(errorMessage));
    }

    #endregion Actions

    #region Data
    
    private bool _isInStickerMode;
    public bool IsInStickerMode 
    {
        get => _isInStickerMode;
        set
        {
            _isInStickerMode = value;
            if (_isInStickerMode) CohtmlHud.Instance.SelectPropToSpawn(BtkUiAddon.GetBtkUiCachePath(ModSettings.Hidden_SelectedStickerName.Value), ModSettings.Hidden_SelectedStickerName.Value, "Sticker selected for stickering:");
            else CohtmlHud.Instance.ClearPropToSpawn();
        }
    }

    private const float StickerKillTime = 30f;
    private const float StickerCooldown = 0.2f;
    private readonly Dictionary<string, StickerData> _playerStickers = new();
    private const string PlayerLocalId = "_PLAYERLOCAL";
    
    private readonly List<StickerData> _deadStickerPool = new(); // for cleanup on player leave
    
    #endregion Data

    #region Game Events

    private void OnPlayerSetupStart()
    {        
        CVRGameEventSystem.World.OnUnload.AddListener(_ => Instance.CleanupAllButSelf());
        CVRGameEventSystem.Player.OnJoinEntity.AddListener(Instance.OnPlayerJoined);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(Instance.OnPlayerLeft);
        SchedulerSystem.AddJob(Instance.OnOccasionalUpdate, 10f, 1f);

        LoadImage(ModSettings.Hidden_SelectedStickerName.Value);
    }

    private void OnPlayerJoined(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = -1f;
        stickerData.SetAlpha(1f);
        _deadStickerPool.Remove(stickerData);
    }

    private void OnPlayerLeft(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = Time.time + StickerKillTime;
        stickerData.SetAlpha(1f);
        _deadStickerPool.Add(stickerData);
    }

    private void OnOccasionalUpdate()
    {
        if (_deadStickerPool.Count == 0) 
            return;

        for (var i = _deadStickerPool.Count - 1; i >= 0; i--)
        {
            float currentTime = Time.time;
            StickerData stickerData = _deadStickerPool[i];
            if (stickerData.DeathTime < 0f) 
                continue;

            if (currentTime < stickerData.DeathTime)
            {
                stickerData.SetAlpha(Mathf.Lerp(0f, 1f, (stickerData.DeathTime - currentTime) / StickerKillTime));
                continue;
            }
            
            _playerStickers.Remove(_playerStickers.First(x => x.Value == stickerData).Key);
            _deadStickerPool.RemoveAt(i);
            stickerData.Cleanup();
        }
    }

    #endregion Game Events
    
    #region Local Player

    public void PlaceStickerFromTransform(Transform transform)
    {
        Vector3 controllerForward = transform.forward;
        Vector3 controllerUp = transform.up;
        Vector3 playerUp = PlayerSetup.Instance.transform.up;
        
        // extracting angle of controller ray on forward axis
        Vector3 projectedControllerUp = Vector3.ProjectOnPlane(controllerUp, controllerForward).normalized;
        Vector3 projectedPlayerUp = Vector3.ProjectOnPlane(playerUp, controllerForward).normalized;
        float angle = Vector3.Angle(projectedControllerUp, projectedPlayerUp);
        
        float angleThreshold = ModSettings.Entry_PlayerUpAlignmentThreshold.Value;
        Vector3 targetUp = (angleThreshold != 0f && angle <= angleThreshold) 
            // leave 0.01% of the controller up vector to prevent issues with alignment on floor & ceiling in Desktop
            ? Vector3.Slerp(controllerUp, playerUp, 0.99f) 
            : controllerUp;
        
        PlaceStickerSelf(transform.position, transform.forward, targetUp);
    }

    /// Place own sticker. Network if successful.
    private void PlaceStickerSelf(Vector3 position, Vector3 forward, Vector3 up, bool alignWithNormal = true)
    {
        if (!AttemptPlaceSticker(PlayerLocalId, position, forward, up, alignWithNormal))
            return; // failed
        
        // placed, now network
        ModNetwork.PlaceSticker(position, forward, up);
    }

    /// Clear own stickers. Network if successful.
    public void ClearStickersSelf()
    {
        OnPlayerStickersClear(PlayerLocalId);
        ModNetwork.ClearStickers();
    }

    /// Set own Texture2D for sticker. Network if cusses.
    private void SetTextureSelf(byte[] imageBytes)
    {
        Texture2D texture = new(1, 1); // placeholder
        texture.LoadImage(imageBytes);
        texture.Compress(true); // noachi said to do
        
        OnPlayerStickerTextureReceived(PlayerLocalId, Guid.Empty, texture);
        if (ModNetwork.SetTexture(imageBytes)) ModNetwork.SendTexture();
    }

    #endregion Local Player

    #region Public Methods

    /// When a player wants to place a sticker.
    public void OnPlayerStickerPlace(string playerId, Vector3 position, Vector3 forward, Vector3 up)
        => AttemptPlaceSticker(playerId, position, forward, up);
    
    /// When a player wants to clear their stickers.
    public void OnPlayerStickersClear(string playerId)
        => ClearStickersForPlayer(playerId);

    /// Clear all stickers from all players.
    public void ClearAllStickers()
    {
        foreach (StickerData stickerData in _playerStickers.Values)
            stickerData.Clear();
        
        ModNetwork.ClearStickers();
    }

    public void OnPlayerStickerTextureReceived(string playerId, Guid textureHash, Texture2D texture)
    {
        StickerData stickerData = GetOrCreateStickerData(playerId);
        stickerData.SetTexture(textureHash, texture);
    }
    
    public Guid GetPlayerStickerTextureHash(string playerId)
    {
        StickerData stickerData = GetOrCreateStickerData(playerId);
        return stickerData.TextureHash;
    }

    public void CleanupAll()
    {
        foreach ((_, StickerData data) in _playerStickers)
            data.Cleanup();
        
        _playerStickers.Clear();
    }

    public void CleanupAllButSelf()
    {
        StickerData localStickerData = GetOrCreateStickerData(PlayerLocalId);
        
        foreach ((_, StickerData data) in _playerStickers)
        {
            if (data.IsLocal) data.Clear();
            else data.Cleanup();
        }
        
        _playerStickers.Clear();
        _playerStickers[PlayerLocalId] = localStickerData;
    }
    
    
    #endregion Public Methods

    #region Private Methods
    
    private StickerData GetOrCreateStickerData(string playerId)
    {
        if (_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return stickerData;
        
        stickerData = new StickerData(playerId == PlayerLocalId);
        _playerStickers[playerId] = stickerData;
        return stickerData;
    }
    
    private bool AttemptPlaceSticker(string playerId, Vector3 position, Vector3 forward, Vector3 up, bool alignWithNormal = true)
    {
        StickerData stickerData = GetOrCreateStickerData(playerId);
        if (Time.time - stickerData.LastPlacedTime < StickerCooldown)
            return false;

        // Every layer other than IgnoreRaycast, PlayerLocal, PlayerClone, PlayerNetwork, and UI Internal
        const int LayerMask = ~((1 << 2) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 15));
        if (!Physics.Raycast(position, forward, out RaycastHit hit, 
                10f, LayerMask, QueryTriggerInteraction.Ignore)) 
            return false;
        
        stickerData.Place(hit, alignWithNormal ? -hit.normal : forward, up);
        stickerData.PlayAudio();
        return true;
    }
    
    private void ClearStickersForPlayer(string playerId)
    {
        if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return;
        
        stickerData.Clear();
    }
    
    private void ReleaseStickerData(string playerId)
    {
        if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return;
        
        stickerData.Cleanup();
        _playerStickers.Remove(playerId);
    }
    
    #endregion Private Methods

    #region Image Loading
    
    private static readonly string s_StickersSourcePath = Application.dataPath + "/../UserData/Stickers/";

    private bool _isLoadingImage;
    
    public void LoadImage(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return;

        if (_isLoadingImage) return;
        _isLoadingImage = true;

        Task.Run(() =>
        {
            try
            {
                if (!TryLoadImage(imageName, out string errorMessage))
                {
                    InvokeOnImageLoadFailed(errorMessage);
                }
            }
            catch (Exception ex)
            {
                InvokeOnImageLoadFailed(ex.Message);
            }
            finally
            {
                _isLoadingImage = false;
            }
        });
    }

    private bool TryLoadImage(string imageName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (ModNetwork.IsSendingTexture)
        {
            StickerMod.Logger.Warning("A texture is currently being sent over the network. Cannot load a new image yet.");
            errorMessage = "A texture is currently being sent over the network. Cannot load a new image yet.";
            return false;
        }

        if (!Directory.Exists(s_StickersSourcePath)) Directory.CreateDirectory(s_StickersSourcePath);

        string imagePath = Path.Combine(s_StickersSourcePath, imageName + ".png");
        FileInfo fileInfo = new(imagePath);
        if (!fileInfo.Exists)
        {
            StickerMod.Logger.Warning($"Target image does not exist on disk. Path: {imagePath}");
            errorMessage = "Target image does not exist on disk.";
            return false;
        }

        var bytes = File.ReadAllBytes(imagePath);

        if (!ImageUtility.IsValidImage(bytes))
        {
            StickerMod.Logger.Error("File is not a valid image or is corrupt.");
            errorMessage = "File is not a valid image or is corrupt.";
            return false;
        }

        StickerMod.Logger.Msg("Loaded image from disk. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");

        if (bytes.Length > ModNetwork.MaxTextureSize)
        {
            ImageUtility.Resize(ref bytes, 256, 256);
            StickerMod.Logger.Warning("File ate too many cheeseburgers. Attempting experimental resize. Notice: this may cause filesize to increase.");
            StickerMod.Logger.Msg("Resized image. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
        }

        if (ImageUtility.ResizeToNearestPowerOfTwo(ref bytes))
        {
            StickerMod.Logger.Warning("Image resolution was not a power of two. Attempting experimental resize. Notice: this may cause filesize to increase.");
            StickerMod.Logger.Msg("Resized image. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
        }

        if (bytes.Length > ModNetwork.MaxTextureSize)
        {
            StickerMod.Logger.Error("File is still too large. Aborting. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
            StickerMod.Logger.Msg("Please resize the image manually to be smaller than " + ModNetwork.MaxTextureSize / 1024 + " KB and round resolution to nearest power of two.");
            errorMessage = "File is still too large. Please resize the image manually to be smaller than " + ModNetwork.MaxTextureSize / 1024 + " KB and round resolution to nearest power of two.";
            return false;
        }

        StickerMod.Logger.Msg("Image successfully loaded.");

        MTJobManager.RunOnMainThread("StickersSystem.LoadImage", () =>
        {
            ModSettings.Hidden_SelectedStickerName.Value = imageName;
            SetTextureSelf(bytes);

            if (!IsInStickerMode) return;
            IsInStickerMode = false;
            IsInStickerMode = true;
        });

        return true;
    }

    public static void OpenStickersFolder()
    {
        if (!Directory.Exists(s_StickersSourcePath)) Directory.CreateDirectory(s_StickersSourcePath);
        Process.Start(s_StickersSourcePath);
    }
    
    public static string GetStickersFolderPath()
    {
        if (!Directory.Exists(s_StickersSourcePath)) Directory.CreateDirectory(s_StickersSourcePath);
        return s_StickersSourcePath;
    }

    #endregion Image Loading
}