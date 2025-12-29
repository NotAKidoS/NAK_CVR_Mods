using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;
using ABI.CCK.Components;
using NAK.Stickers.Integrations;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Singleton
    
    public static StickerSystem Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
            return;
        
        Instance = new StickerSystem();
        
        // configure decalery
        DecalManager.SetPreferredMode(DecalUtils.Mode.GPU, false, 0);
        
        // ensure cache folder exists
        EnsureStickersFolderExists();
        
        // listen for game events
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Instance.OnPlayerSetupStart);

}
    
    #endregion Singleton

    #region Callback Registration
    
    private void OnPlayerSetupStart()
    {
        // TODO: this can be spammed by world author toggling CVRWorld.enabled state
        CVRGameEventSystem.World.OnLoad.AddListener(_ => OnWorldLoad()); 
        CVRGameEventSystem.World.OnUnload.AddListener(_ => OnWorldUnload());
        CVRGameEventSystem.Instance.OnConnected.AddListener((_) => { if (!Instances.IsReconnecting) OnInitialConnection(); });
        
        CVRGameEventSystem.Player.OnJoinEntity.AddListener(Instance.OnPlayerJoined);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(Instance.OnPlayerLeft);
        BetterScheduleSystem.AddJob(Instance.OnUpdate, 10f, -1);
        LoadAllImagesAtStartup();
    }

    #endregion Callback Registration
    
    #region Game Events
    
    private void OnInitialConnection()
    {
        ClearStickersSelf(); // clear stickers on remotes just in case we rejoined
        ModNetwork.Reset(); // reset network buffers and metadata
    }

    private void OnWorldLoad()
    {
        CVRDataStore worldDS = CVRWorld.Instance.DataStore;
        // IsRestrictedInstance = worldDS && worldDS.GetValue<bool>("StickersMod-ForceDisable");
        if (IsRestrictedInstance) StickerMod.Logger.Msg("Stickers are restricted by the world author.");
        BTKUIAddon.OnStickerRestrictionUpdated(IsRestrictedInstance);
    }

    private void OnWorldUnload()
    {
        IsRestrictedInstance = false;
        CleanupAllButSelf();
    }
    
    #endregion Game Events

    #region Data

    // private bool _isEnabled = true;
    //
    // public bool IsEnabled
    // {
    //     get => _isEnabled;
    //     set
    //     {
    //         if (_isEnabled == value) 
    //             return;
    //         
    //         _isEnabled = value;
    //         if (!_isEnabled) ClearAllStickers();
    //         ModNetwork.IsEnabled = _isEnabled;
    //     }
    // }
    
    public bool IsRestrictedInstance { get; internal set; }
    
    private string SelectedStickerName => ModSettings.Hidden_SelectedStickerNames.Value[_selectedStickerSlot];

    private const float StickerKillTime = 30f;
    private const float StickerCooldown = 0.2f;
    private readonly Dictionary<string, StickerData> _playerStickers = new();
    internal const string PlayerLocalId = "_PLAYERLOCAL";
    
    private int _selectedStickerSlot;
    public int SelectedStickerSlot
    {
        get => _selectedStickerSlot;
        set
        {
            _selectedStickerSlot = value < 0 ? ModSettings.MaxStickerSlots - 1 : value % ModSettings.MaxStickerSlots;
            IsInStickerMode = IsInStickerMode; // refresh sticker mode
        }
    }
    
    private bool _isInStickerMode;
    public bool IsInStickerMode 
    {
        get => _isInStickerMode;
        set
        {
            _isInStickerMode = value && !IsRestrictedInstance; // ensure cannot enter when restricted
            if (_isInStickerMode)
            {
                CohtmlHud.Instance.SelectPropToSpawn(
                StickerCache.GetCohtmlResourcesPath(SelectedStickerName),
                Path.GetFileNameWithoutExtension(SelectedStickerName), 
                "Sticker selected for stickering:");
            }
            else
            {
                CohtmlHud.Instance.ClearPropToSpawn();
                ClearStickerPreview();
            }
        }
    }
    
    #endregion Data
}