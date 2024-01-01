using System.Collections;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.AvatarTracking;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using NAK.AvatarScaleMod.Components;
using NAK.AvatarScaleMod.Networking;
using UnityEngine;

namespace NAK.AvatarScaleMod.AvatarScaling;

public class AvatarScaleManager : MonoBehaviour
{
    public static AvatarScaleManager Instance { get; private set; }
    
    private LocalScaler _localAvatarScaler;
    private Dictionary<string, NetworkScaler> _networkedScalers;
    
    private Coroutine _heightUpdateCoroutine;
    private readonly YieldInstruction _heightUpdateYield = new WaitForEndOfFrame();
    
    #region Universal Scaling Limits

    // ReSharper disable MemberCanBePrivate.Global
    // To match AvatarScaleTool: https://github.com/NotAKidOnSteam/AvatarScaleTool/tree/main
    public const float DefaultMinHeight = 0.25f;
    public const float DefaultMaxHeight = 2.50f;
    // ReSharper restore MemberCanBePrivate.Global
    
    // Universal Scaling Limits
    public static float MinHeight { get; private set; } = DefaultMinHeight;
    public static float MaxHeight { get; private set; } = DefaultMaxHeight;

    #endregion
    
    #region Settings

    private bool _settingUniversalScaling;
    public bool Setting_UniversalScaling
    {
        get => _settingUniversalScaling;
        set
        {
            if (value != _settingUniversalScaling && value == false)
                ResetTargetHeight();
            
            _settingUniversalScaling = value;
            SetTargetHeight(_lastTargetHeight); // immediate height update
        }
    }

    public bool Setting_AnimationClipScalingOverride;
    public bool Setting_PersistentHeight;
    private float _lastTargetHeight = -1;

    #endregion
    
    #region Unity Events

    private void Awake()
    {
        if (Instance != null
            && Instance != this)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;
        _networkedScalers = new Dictionary<string, NetworkScaler>();
    }

    private void Start()
    {
        _localAvatarScaler = PlayerSetup.Instance.gameObject.AddComponent<LocalScaler>();
        _localAvatarScaler.Initialize();
        
        _settingUniversalScaling = ModSettings.EntryUseUniversalScaling.Value;
        Setting_AnimationClipScalingOverride = ModSettings.EntryAnimationScalingOverride.Value;
        Setting_PersistentHeight = ModSettings.EntryPersistentHeight.Value;
        _lastTargetHeight = ModSettings.EntryPersistThroughRestart.Value 
            ? ModSettings.EntryHiddenAvatarHeight.Value : -1f; // -1f is default
        
        // listen for events
        _localAvatarScaler.OnAnimatedHeightOverride += OnAnimationHeightOverride;
        
        CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);
    }
    
    private void OnEnable()
    {
        if (_heightUpdateCoroutine != null) StopCoroutine(_heightUpdateCoroutine);
        _heightUpdateCoroutine = StartCoroutine(HeightUpdateCoroutine());
    }
    
    private void OnDisable()
    {
        if (_heightUpdateCoroutine != null) StopCoroutine(_heightUpdateCoroutine);
        _heightUpdateCoroutine = null;
    }

    private void OnDestroy()
    {
        _heightUpdateCoroutine = null;
        CVRGameEventSystem.Instance.OnConnected.RemoveListener(OnInstanceConnected);

        if (_localAvatarScaler != null) Destroy(_localAvatarScaler);
        _localAvatarScaler = null;
        
        foreach (NetworkScaler scaler in _networkedScalers.Values) Destroy(scaler);
        _networkedScalers.Clear();
        
        if (Instance == this)
            Instance = null;
    }
    
    // only update the height per scaler once per frame, to prevent spam & jitter
    // this is to ensure that the height is also set at correct time during frame, no matter when it is called
    private IEnumerator HeightUpdateCoroutine()
    {
        while (enabled)
        {
            yield return _heightUpdateYield;
            
            // update local scaler
            if (_localAvatarScaler != null && _localAvatarScaler.heightNeedsUpdate)
            {
                if (_localAvatarScaler.ApplyTargetHeight())
                    AvatarScaleEvents.OnLocalAvatarHeightChanged.Invoke(_localAvatarScaler);
            }

            // update networked scalers (probably a better way to do this)
            foreach (var netScaler in _networkedScalers)
            {
                if (!netScaler.Value.heightNeedsUpdate) continue;
                if (netScaler.Value.ApplyTargetHeight())
                    AvatarScaleEvents.OnRemoteAvatarHeightChanged.Invoke(netScaler.Key, netScaler.Value);
            }
        }
        
        // ReSharper disable once IteratorNeverReturns
    }

    #endregion

    #region Game Events

    public void OnInstanceConnected(string instanceId)
    {
        // TODO: need to know if this causes issues when in a reconnection loop
        SchedulerSystem.AddJob(ModNetwork.RequestHeightSync, 2f, 1f, 1);
    }
    
    #endregion

    #region Local Methods

    public void OnAvatarInstantiated(PlayerSetup playerSetup)
    {
        if (playerSetup._avatar == null)
            return;

        _localAvatarScaler.OnAvatarInstantiated(playerSetup._avatar, playerSetup._initialAvatarHeight,
            playerSetup.initialScale);
        
        if (!_settingUniversalScaling)
            return;

        SetTargetHeight(_lastTargetHeight);
    }

    public void OnAvatarDestroyed(PlayerSetup playerSetup)
    {
        if (_localAvatarScaler != null)
            _localAvatarScaler.OnAvatarDestroyed();
    }

    public void SetTargetHeight(float targetHeight)
    {
        _lastTargetHeight = targetHeight; // save for persistent height
        ModSettings.EntryHiddenAvatarHeight.Value = targetHeight; // save for restart

        if (!_settingUniversalScaling)
            return;
        
        if (_localAvatarScaler == null)
            return;
        
        _localAvatarScaler.SetTargetHeight(_lastTargetHeight);
        _localAvatarScaler.heightNeedsUpdate = true; // only local scaler forces update
    }
    
    public void ResetTargetHeight()
    {
        if (_localAvatarScaler == null)
            return;

        if (!_localAvatarScaler.IsForcingHeight())
            return;

        // TODO: doesnt work when hitting Reset on slider in BTK UI (is it on main thread?)
        CohtmlHud.Instance.ViewDropTextImmediate("(Local) AvatarScaleMod", "Avatar Scale Reset!",
            "Universal Scaling is now disabled.");
        
        SetTargetHeight(-1f);
    }

    public float GetHeight()
    {
        if (_localAvatarScaler == null)
            return PlayerAvatarPoint.defaultAvatarHeight;

        if (!_localAvatarScaler.IsForcingHeight())
            return PlayerSetup.Instance.GetAvatarHeight();
        
        return _localAvatarScaler.GetTargetHeight();
    }
    
    public float GetAnimationClipHeight()
    {
        if (_localAvatarScaler == null)
            return PlayerAvatarPoint.defaultAvatarHeight;

        if (!_localAvatarScaler.IsForcingHeight())
            return PlayerSetup.Instance.GetAvatarHeight();
        
        return _localAvatarScaler.GetAnimatedHeight();
    }
    
    public float GetHeightForNetwork()
    {
        if (!_settingUniversalScaling)
            return -1f;
        
        if (_localAvatarScaler == null)
            return -1f;
        
        if (!_localAvatarScaler.IsForcingHeight())
            return -1f;

        return _localAvatarScaler.GetTargetHeight();
    }

    public float GetInitialHeight()
    {
        if (_localAvatarScaler == null)
            return -1f;

        return _localAvatarScaler.GetInitialHeight();
    }

    public bool IsHeightAdjustedFromInitial()
    {
        return _localAvatarScaler != null && _localAvatarScaler.IsForcingHeight();
    }

    #endregion

    #region Network Methods

    public bool DoesNetworkHeightScalerExist(string playerId)
        => _networkedScalers.ContainsKey(playerId);
    
    public int GetNetworkHeightScalerCount()
        => _networkedScalers.Count;
    
    public float GetNetworkHeight(string playerId)
    {
        if (_networkedScalers.TryGetValue(playerId, out NetworkScaler scaler))
            if (scaler.IsForcingHeight()) return scaler.GetTargetHeight();
        
        //doesn't have mod or has no custom height, get from player avatar directly
        CVRPlayerEntity playerEntity = CVRPlayerManager.Instance.NetworkPlayers.Find((players) => players.Uuid == playerId);
        if (playerEntity != null && playerEntity.PuppetMaster != null)
            return playerEntity.PuppetMaster.GetAvatarHeight();
        
        // player is invalid???
        return -1f;
    }

    // we will create a Universal Scaler for only users that send a height update
    // this is sent at a rate of 10s locally!

    internal void OnNetworkHeightUpdateReceived(string playerId, float targetHeight)
    {
        if (_networkedScalers.TryGetValue(playerId, out NetworkScaler scaler))
            scaler.SetTargetHeight(targetHeight);
        else
            SetupHeightScalerForNetwork(playerId, targetHeight);
    }

    internal void OnNetworkAvatarInstantiated(PuppetMaster puppetMaster)
    {
        var playerId = puppetMaster._playerDescriptor.ownerId;
        if (_networkedScalers.TryGetValue(playerId, out NetworkScaler scaler))
            scaler.OnAvatarInstantiated(puppetMaster.avatarObject, puppetMaster._initialAvatarHeight,
                puppetMaster.initialAvatarScale);
    }

    internal void OnNetworkAvatarDestroyed(PuppetMaster puppetMaster)
    {
        // on disconnect
        if (puppetMaster == null || puppetMaster._playerDescriptor == null)
            return;

        var playerId = puppetMaster._playerDescriptor.ownerId;
        if (_networkedScalers.TryGetValue(playerId, out NetworkScaler scaler))
            scaler.OnAvatarDestroyed();
    }

    internal void RemoveNetworkHeightScaler(string playerId)
    {
        if (_networkedScalers.ContainsKey(playerId))
        {
            AvatarScaleMod.Logger.Msg(
                $"Removed user height scaler! This is hopefully due to a disconnect or block. : {playerId}");

            _networkedScalers.Remove(playerId);
            return;
        }

        AvatarScaleMod.Logger.Msg(
            $"Failed to remove a user height scaler! This shouldn't happen. : {playerId}");
    }

    private void SetupHeightScalerForNetwork(string playerId, float targetHeight)
    {
        CVRPlayerEntity playerEntity =
            CVRPlayerManager.Instance.NetworkPlayers.Find(players => players.Uuid == playerId);

        PuppetMaster puppetMaster = playerEntity?.PuppetMaster;

        if (playerEntity == null || puppetMaster == null)
        {
            AvatarScaleMod.Logger.Error(
                $"Attempted to set up height scaler for user which does not exist! : {playerId}");
            return;
        }

        AvatarScaleMod.Logger.Msg(
            $"Setting up new height scaler for user which has sent a height update! : {playerId}");

        if (_networkedScalers.ContainsKey(playerId))
            _networkedScalers.Remove(playerId); // ??

        NetworkScaler scaler = puppetMaster.gameObject.AddComponent<NetworkScaler>();
        scaler.Initialize(playerId);

        scaler.OnAvatarInstantiated(puppetMaster.avatarObject, puppetMaster._initialAvatarHeight,
            puppetMaster.initialAvatarScale);

        _networkedScalers[playerId] = scaler;

        scaler.SetTargetHeight(targetHeight); // set initial height
    }

    #endregion

    #region Manager Methods
    
    // sometimes fun to play with via UE
    public void SetUniversalScalingLimit(float min, float max)
    {
        const float HardCodedMinLimit = 0.01f;
        const float HardCodedMaxLimit = 100f;
        
        MinHeight = Mathf.Clamp(min, HardCodedMinLimit, HardCodedMaxLimit);
        MaxHeight = Mathf.Clamp(max, HardCodedMinLimit, HardCodedMaxLimit);
        
        AvatarScaleMod.Logger.Msg($"Universal Scaling Limits changed: {min} - {max}");
        AvatarScaleMod.Logger.Warning("This will not network to other users unless they also have the same limits set!");
    }
    
    public void ResetUniversalScalingLimit()
    {
        MinHeight = DefaultMinHeight;
        MaxHeight = DefaultMaxHeight;
        
        AvatarScaleMod.Logger.Msg("Universal Scaling Limits reset to default!");
    }

    #endregion

    #region Event Listeners
    
    private static void OnAnimationHeightOverride(BaseScaler scaler)
    {
        AvatarScaleMod.Logger.Msg("AnimationClip-based avatar scaling detected. Disabling Universal Scaling.");
        CohtmlHud.Instance.ViewDropTextImmediate("(Local) AvatarScaleMod", "Avatar Scale Changed!",
            "Universal Scaling is now disabled in favor of built-in avatar scaling.");
    }

    #endregion
}