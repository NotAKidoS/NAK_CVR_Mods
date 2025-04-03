using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Savior;

namespace NAK.BetterContentLoading;

public partial class DownloadManager
{
    #region Singleton
    private static DownloadManager _instance;
    public static DownloadManager Instance => _instance ??= new DownloadManager();
    #endregion

    private DownloadManager()
    {
        
    }
    
    #region Settings
    public bool IsDebugEnabled { get; set; } = true;
    public bool PrioritizeFriends { get; set; } = true;
    public bool PrioritizeDistance { get; set; } = true;
    public float PriorityDownloadDistance { get; set; } = 25f;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public int MaxDownloadBandwidth { get; set; } = 100 * 1024 * 1024; // 100MB default
    private const int THROTTLE_THRESHOLD = 25 * 1024 * 1024; // 25MB threshold for throttling
    
    public long MaxAvatarDownloadSize { get; set; } = 25 * 1024 * 1024; // 25MB default
    public long MaxPropDownloadSize { get; set; } = 25 * 1024 * 1024; // 25MB default
    
    #endregion Settings

    #region State
    
    // priority -> downloadtask
    private readonly SortedList<float, DownloadTask> _downloadQueue = new();
    
    // downloadId -> downloadtask
    private readonly Dictionary<string, DownloadTask> _cachedDownloads = new();

    #endregion State

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
        
        if (!ShouldQueueAvatarDownload(info, playerId))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Avatar Download not eligible: {info.DownloadId}");
            return;
        }
        
        if (_cachedDownloads.TryGetValue(info.DownloadId, out DownloadTask cachedDownload))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Avatar Download already queued: {info.DownloadId}");
            cachedDownload.AddInstantiationTarget(playerId);
            cachedDownload.BasePriority = CalculatePriority(cachedDownload);
            return;
        }
        
        DownloadTask task = new()
        {
            Info = info,
            Type = DownloadTaskType.Avatar
        };
        task.AddInstantiationTarget(playerId);
        task.BasePriority = CalculatePriority(task);
    }
    
    private bool ShouldQueueAvatarDownload(
        in DownloadInfo info,
        string playerId)
    {
        // Check if content is incompatible or banned
        if (info.TagsData.Incompatible || info.TagsData.AdminBanned)
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Avatar is incompatible or banned");
            return false;
        }
        
        // Check if player is blocked
        if (MetaPort.Instance.blockedUserIds.Contains(playerId))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Player is blocked: {playerId}");
            return false;
        }
        
        // Check if mature content is disabled
        UgcTagsData tags = info.TagsData;
        if (!MetaPort.Instance.matureContentAllowed && (tags.Gore || tags.Nudity))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Mature content is disabled");
            return false;
        }
        
        // Check file size
        if (info.FileSize > MaxAvatarDownloadSize)
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Avatar Download too large: {info.FileSize} > {MaxAvatarDownloadSize}");
            return false;
        }
        
        // Get visibility status for the avatar
        // ForceHidden means player avatar or avatar itself is forced off.
        // ForceShown will bypass all checks and return true.
        MetaPort.Instance.SelfModerationManager.GetAvatarVisibility(playerId, info.AssetId,
            out bool wasForceHidden, out bool wasForceShown);
        
        if (!wasForceHidden)
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Avatar is not visible either because player avatar or avatar itself is forced off");
            return false;
        }

        if (!wasForceShown)
        {
            // Check content filter settings if not force shown
            CVRSettings settings = MetaPort.Instance.settings;
            bool isLocalPlayer = playerId == MetaPort.Instance.ownerId;
            bool isFriend = Friends.FriendsWith(playerId);
            bool CheckFilterSettings(string settingName)
            {
                int settingVal = settings.GetSettingInt(settingName);
                switch (settingVal)
                {
                    // Only Self 
                    case 0 when !isLocalPlayer:
                    // Only Friends
                    case 1 when !isFriend:
                        return false;
                }
                return true;
            }
        
            if (!CheckFilterSettings("ContentFilterVisibility")) return false;
            if (!CheckFilterSettings("ContentFilterNudity")) return false;
            if (!CheckFilterSettings("ContentFilterGore")) return false;
            if (!CheckFilterSettings("ContentFilterSuggestive")) return false;
            if (!CheckFilterSettings("ContentFilterFlashingColors")) return false;
            if (!CheckFilterSettings("ContentFilterFlashingLights")) return false;
            if (!CheckFilterSettings("ContentFilterScreenEffects")) return false;
            if (!CheckFilterSettings("ContentFilterExtremelyBright")) return false;
            if (!CheckFilterSettings("ContentFilterViolence")) return false;
            if (!CheckFilterSettings("ContentFilterJumpscare")) return false;
            if (!CheckFilterSettings("ContentFilterExcessivelyHuge")) return false;
            if (!CheckFilterSettings("ContentFilterExcessivelySmall")) return false;
        }
        
        // All eligibility checks passed
        return true;
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
        
        if (_cachedDownloads.TryGetValue(info.DownloadId, out DownloadTask cachedDownload))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"Prop Download already queued: {info.DownloadId}");
            cachedDownload.AddInstantiationTarget(instanceId);
            cachedDownload.BasePriority = CalculatePriority(cachedDownload);
            return;
        }
        
        DownloadTask task = new()
        {
            Info = info,
            Type = DownloadTaskType.Prop
        };
        task.AddInstantiationTarget(instanceId);
        task.BasePriority = CalculatePriority(task);
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
        
        if (_cachedDownloads.TryGetValue(info.DownloadId, out DownloadTask cachedDownload))
        {
            if (IsDebugEnabled) BetterContentLoadingMod.Logger.Msg($"World Download already queued: {info.DownloadId}");
            cachedDownload.BasePriority = CalculatePriority(cachedDownload);
            return;
        }
        
        DownloadTask task = new()
        {
            Info = info,
            Type = DownloadTaskType.World
        };
        task.BasePriority = CalculatePriority(task);
    }
    
    #endregion Public Queue Methods

}