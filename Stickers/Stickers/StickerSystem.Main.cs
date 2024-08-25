using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using NAK.Stickers.Utilities;
using UnityEngine;

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
        if (!Directory.Exists(s_StickersFolderPath)) Directory.CreateDirectory(s_StickersFolderPath);
        
        // listen for game events
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Instance.OnPlayerSetupStart);
    }
    
    #endregion Singleton

    #region Data

    private int _selectedStickerSlot;
    public int SelectedStickerSlot
    {
        get => _selectedStickerSlot;
        set
        {
            _selectedStickerSlot = Mathf.Clamp(value, 0, ModSettings.MaxStickerSlots - 1);
            IsInStickerMode = IsInStickerMode; // refresh sticker mode
        }
    }
    
    private bool _isInStickerMode;
    public bool IsInStickerMode 
    {
        get => _isInStickerMode;
        set
        {
            _isInStickerMode = value;
            if (_isInStickerMode) CohtmlHud.Instance.SelectPropToSpawn(
                StickerCache.GetCohtmlResourcesPath(SelectedStickerName),
                Path.GetFileNameWithoutExtension(SelectedStickerName), 
                "Sticker selected for stickering:");
            else CohtmlHud.Instance.ClearPropToSpawn();
        }
    }

    private string SelectedStickerName => ModSettings.Hidden_SelectedStickerNames.Value[_selectedStickerSlot];

    private const float StickerKillTime = 30f;
    private const float StickerCooldown = 0.2f;
    private readonly Dictionary<string, StickerData> _playerStickers = new();
    private const string PlayerLocalId = "_PLAYERLOCAL";
    
    private readonly List<StickerData> _deadStickerPool = new(); // for cleanup on player leave

    #endregion Data
}