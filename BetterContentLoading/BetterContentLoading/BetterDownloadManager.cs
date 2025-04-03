using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Systems.GameEventSystem;
using NAK.BetterContentLoading.Queue;
using UnityEngine;

namespace NAK.BetterContentLoading;

// Download world -> connect to instance -> receive all Avatar data
// -> initial connection to instance -> receive all props data

// We receive Prop download data only after we have connected to the instance. Avatar data we seem to receive
// prior to our initial connection event firing.

public class BetterDownloadManager
{
    #region Singleton
    
    private static BetterDownloadManager _instance;
    public static BetterDownloadManager Instance => _instance ??= new BetterDownloadManager();

    #endregion Singleton
    
    #region Constructor
    
    private BetterDownloadManager()
    {
        _downloadProcessor = new DownloadProcessor();
        
        _worldQueue = new WorldDownloadQueue(this); // Only one world at a time
        _avatarQueue = new AvatarDownloadQueue(this); // Up to 3 avatars at once
        _propQueue = new PropDownloadQueue(this);   // Up to 2 props at once
        
        // Set to 100MBs by default
        MaxDownloadBandwidth = 100 * 1024 * 1024;
        
        CVRGameEventSystem.Instance.OnConnected.AddListener(_ =>
        {
            if (!Instances.IsReconnecting) OnInitialConnectionToInstance(); 
        });
    }

    #endregion Constructor

    #region Settings

    /// Log debug messages
    public bool IsDebugEnabled { get; set; } = true;
    
    /// Prioritize friends first in download queue
    public bool PrioritizeFriends { get; set; } = true;
    
    /// Prioritize content closest to player first in download queue
    public bool PrioritizeDistance { get; set; } = true;
    public float PriorityDownloadDistance { get; set; } = 25f;
    
    public int MaxDownloadBandwidth
    {
        get => _downloadProcessor.MaxDownloadBandwidth;
        set => _downloadProcessor.MaxDownloadBandwidth = value;
    }
    
    #endregion Settings
    
    private readonly DownloadProcessor _downloadProcessor;

    private readonly AvatarDownloadQueue _avatarQueue;
    private readonly PropDownloadQueue _propQueue;
    private readonly WorldDownloadQueue _worldQueue;

    #region Game Events

    private void OnInitialConnectionToInstance()
    {
        if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg("Initial connection established.");
        // await few seconds before chewing through the download queue, to allow for download priorities to be set
        // once we have received most of the data from the server
    }

    #endregion Game Events
    
    #region Public Queue Methods
    
    public void QueueAvatarDownload(
        in DownloadInfo info,
        string playerId,
        CVRLoadingAvatarController loadingController)
    {
        if (IsDebugEnabled)
        {
            BetterContentLoadingMod.Logger.Msg(
                $"Queuing Avatar Download:\n{info.GetLogString()}\n" +
                $"PlayerId: {playerId}\n" +
                $"LoadingController: {loadingController}");
        }
        
        _avatarQueue.QueueDownload(in info, playerId);
    }

    /// <summary>
    /// Queues a prop download.
    /// </summary>
    /// <param name="info">The download info.</param>
    /// <param name="instanceId">The instance ID for the prop.</param>
    /// <param name="spawnerId">The user who spawned the prop.</param>
    public void QueuePropDownload(
        in DownloadInfo info,
        string instanceId,
        string spawnerId)
    {
        if (IsDebugEnabled)
        {
            BetterContentLoadingMod.Logger.Msg(
                $"Queuing Prop Download:\n{info.GetLogString()}\n" +
                $"InstanceId: {instanceId}\n" +
                $"SpawnerId: {spawnerId}");
        }
        
        _propQueue.QueueDownload(in info, instanceId, spawnerId);
    }
    
    /// <summary>
    /// Queues a world download.
    /// </summary>
    /// <param name="info">Download info.</param>
    /// <param name="joinOnComplete">Whether to load into this world once downloaded.</param>
    /// <param name="isHomeRequested">Whether the home world is requested.</param>
    public void QueueWorldDownload(
        in DownloadInfo info,
        bool joinOnComplete,
        bool isHomeRequested)
    {
        if (IsDebugEnabled)
        {
            BetterContentLoadingMod.Logger.Msg(
                $"Queuing World Download:\n{info.GetLogString()}\n" +
                $"JoinOnComplete: {joinOnComplete}\n" +
                $"IsHomeRequested: {isHomeRequested}");
        }
        
        _worldQueue.QueueDownload(in info, joinOnComplete, isHomeRequested);
    }
    
    #endregion Public Queue Methods

    #region Internal Methods

    internal Task<bool> ProcessDownload(DownloadInfo info, Action<float> progressCallback = null)
    {
        return _downloadProcessor.ProcessDownload(info);
    }

    #endregion Internal Methods

    #region Private Helper Methods

    internal static bool TryGetPlayerEntity(string playerId, out CVRPlayerEntity playerEntity)
    {
        CVRPlayerEntity player = CVRPlayerManager.Instance.NetworkPlayers.Find(p => p.Uuid == playerId);
        if (player == null)
        {
            // BetterContentLoadingMod.Logger.Error($"Player entity not found for ID: {playerId}");
            playerEntity = null;
            return false;
        }
        playerEntity = player;
        return true;
    }
    
    internal static bool TryGetPropData(string instanceId, out CVRSyncHelper.PropData propData)
    {
        CVRSyncHelper.PropData prop = CVRSyncHelper.Props.Find(p => p.InstanceId == instanceId);
        if (prop == null)
        {
            // BetterContentLoadingMod.Logger.Error($"Prop data not found for ID: {instanceId}");
            propData = null;
            return false;
        }
        propData = prop;
        return true;
    }
    
    internal static bool IsPlayerLocal(string playerId)
    {
        return playerId == MetaPort.Instance.ownerId;
    }
    
    internal static bool IsPlayerFriend(string playerId)
    {
        return Friends.FriendsWith(playerId);
    }
    
    internal bool IsPlayerWithinPriorityDistance(CVRPlayerEntity player)
    {
        if (player.PuppetMaster == null) return false;
        return player.PuppetMaster.animatorManager.DistanceTo < PriorityDownloadDistance;
    }
    
    internal bool IsPropWithinPriorityDistance(CVRSyncHelper.PropData prop)
    {
        Vector3 propPosition = new(prop.PositionX, prop.PositionY, prop.PositionZ);
        return Vector3.Distance(propPosition, PlayerSetup.Instance.GetPlayerPosition()) < PriorityDownloadDistance;
    }

    #endregion Private Helper Methods
}