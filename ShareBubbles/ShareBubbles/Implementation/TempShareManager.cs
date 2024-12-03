using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using NAK.ShareBubbles.API;
using Newtonsoft.Json;
using UnityEngine;

namespace NAK.ShareBubbles;

// Debating on whether to keep this as a feature or not
// Is janky and for avatars it causes this:
// https://feedback.abinteractive.net/p/when-avatar-is-unshared-by-owner-remotes-will-see-content-incompatible-bot

public class TempShareManager
{
    #region Constructor

    private TempShareManager()
    {
        string userDataPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserData"));
        savePath = Path.Combine(userDataPath, "sharebubbles_session_shares.json");
    }

    #endregion Constructor
    
    #region Singleton

    public static TempShareManager Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null) return;
        Instance = new TempShareManager();
        
        Instance.LoadShares();
        Instance.InitializeEvents();
    }

    #endregion

    #region Constants & Fields

    private readonly string savePath;
    private readonly object fileLock = new();
    
    private TempShareData shareData = new();
    private readonly HashSet<string> grantedSharesThisSession = new();

    #endregion Constants & Fields

    #region Data Classes

    [Serializable]
    private class TempShare
    {
        public ShareApiHelper.ShareContentType ContentType { get; set; }
        public string ContentId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Serializable]
    private class TempShareData
    {
        public List<TempShare> Shares { get; set; } = new();
    }

    #endregion Data Classes

    #region Public Methods

    public void AddTempShare(ShareApiHelper.ShareContentType contentType, string contentId, string userId)
    {
        ShareBubblesMod.Logger.Msg($"Adding temp share for {userId}...");
        
        TempShare share = new()
        {
            ContentType = contentType,
            ContentId = contentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        // So we can monitor when they leave
        grantedSharesThisSession.Add(userId);

        shareData.Shares.Add(share);
        SaveShares();
    }

    #endregion Public Methods

    #region Event Handlers

    private void InitializeEvents()
    {
        CVRGameEventSystem.Instance.OnConnected.AddListener(OnConnected);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(OnPlayerLeft);
        Application.quitting += OnApplicationQuit;
    }

    private async void OnConnected(string _)
    {
        if (Instances.IsReconnecting) 
            return;

        if (shareData.Shares.Count == 0) 
            return;
        
        ShareBubblesMod.Logger.Msg($"Revoking {shareData.Shares.Count} shares from last session...");
        
        // Attempt to revoke all shares when connecting to an online instance
        // This will catch shares not revoked last session, and in prior instance
        await RevokeAllShares();
        
        ShareBubblesMod.Logger.Msg($"There are {shareData.Shares.Count} shares remaining.");
    }

    private async void OnPlayerLeft(CVRPlayerEntity player)
    {
        // If they were granted shares this session, revoke them
        if (grantedSharesThisSession.Contains(player.Uuid))
            await RevokeSharesForUser(player.Uuid);
    }

    private void OnApplicationQuit()
    {
        // Attempt to revoke all shares when the game closes
        RevokeAllShares().GetAwaiter().GetResult();
    }

    #endregion Event Handlers

    #region Share Management

    private async Task RevokeSharesForUser(string userId)
    {
        for (int i = shareData.Shares.Count - 1; i >= 0; i--)
        {
            TempShare share = shareData.Shares[i];
            if (share.UserId != userId) continue;

            if (!await RevokeShare(share)) 
                continue;
            
            shareData.Shares.RemoveAt(i);
            SaveShares();
        }
    }
    
    private async Task RevokeAllShares()
    {
        for (int i = shareData.Shares.Count - 1; i >= 0; i--)
        {
            if (!await RevokeShare(shareData.Shares[i])) 
                continue;
            
            shareData.Shares.RemoveAt(i);
            SaveShares();
        }
    }

    private async Task<bool> RevokeShare(TempShare share)
    {
        try
        {
            var response = await ShareApiHelper.ReleaseShareAsync<BaseResponse>(
                share.ContentType, 
                share.ContentId, 
                share.UserId
            );
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to revoke share: {ex.Message}");
            return false;
        }
    }

    #endregion Share Management

    #region File Operations

    private void LoadShares()
    {
        try
        {
            lock (fileLock)
            {
                if (!File.Exists(savePath)) 
                    return;
                
                string json = File.ReadAllText(savePath);
                shareData = JsonConvert.DeserializeObject<TempShareData>(json) ?? new TempShareData();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load temp shares: {ex.Message}");
            shareData = new TempShareData();
        }
    }

    private void SaveShares()
    {
        try
        {
            lock (fileLock)
            {
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    if (directory == null) throw new Exception("Failed to get directory path");
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(shareData, Formatting.Indented);
                File.WriteAllText(savePath, json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save temp shares: {ex.Message}");
        }
    }

    #endregion
}