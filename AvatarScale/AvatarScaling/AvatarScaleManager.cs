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
    // Universal Scaling Limits
    public const float MinHeight = 0.1f;
    public const float MaxHeight = 10f;
    
    public static AvatarScaleManager Instance;
    
    private LocalScaler _localAvatarScaler;
    private Dictionary<string, NetworkScaler> _networkedScalers;

    private bool _settingUniversalScaling;
    public bool Setting_UniversalScaling
    {
        get => _settingUniversalScaling;
        set
        {
            if (value != _settingUniversalScaling && value == false)
                ResetHeight();
            
            _settingUniversalScaling = value;
        }
    }

    public bool Setting_PersistantHeight;
    private float _lastTargetHeight = -1;


    #region Unity Methods

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;
        _networkedScalers = new Dictionary<string, NetworkScaler>();
    }

    private void Start()
    {
        _settingUniversalScaling = ModSettings.EntryUseUniversalScaling.Value;
        
        CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);
    }

    private void OnDestroy()
    {
        CVRGameEventSystem.Instance.OnConnected.RemoveListener(OnInstanceConnected);
    }

    #endregion

    #region Events

    public void OnInstanceConnected(string instanceId)
    {
        SchedulerSystem.AddJob(ModNetwork.RequestHeightSync, 2f, 1f, 1);
    }
    
    #endregion

    #region Local Methods

    public void OnAvatarInstantiated(PlayerSetup playerSetup)
    {
        if (playerSetup._avatar == null)
            return;

        if (_localAvatarScaler == null)
        {
            _localAvatarScaler = playerSetup.gameObject.AddComponent<LocalScaler>();
            _localAvatarScaler.Initialize();
        }

        _localAvatarScaler.OnAvatarInstantiated(playerSetup._avatar, playerSetup._initialAvatarHeight,
            playerSetup.initialScale);
        
        if (!_settingUniversalScaling)
            return;

        SetHeight(Setting_PersistantHeight ? _lastTargetHeight : -1f);
    }

    public void OnAvatarDestroyed(PlayerSetup playerSetup)
    {
        if (_localAvatarScaler != null)
            _localAvatarScaler.OnAvatarDestroyed();
    }

    public void SetHeight(float targetHeight)
    {
        if (!_settingUniversalScaling)
            return;
        
        if (_localAvatarScaler == null)
            return;
        
        _lastTargetHeight = targetHeight;
        
        _localAvatarScaler.SetTargetHeight(targetHeight);
        ModNetwork.SendNetworkHeight(targetHeight);

        // immediately update play space scale
        PlayerSetup.Instance.CheckUpdateAvatarScaleToPlaySpaceRelation();
    }

    public void ResetHeight()
    {
        if (_localAvatarScaler == null)
            return;

        if (!_localAvatarScaler.IsHeightAdjustedFromInitial())
            return;

        CohtmlHud.Instance.ViewDropTextImmediate("(Local) AvatarScaleMod", "Avatar Scale Reset!",
            "Universal Scaling is now disabled.");
        
        SetHeight(-1f);
    }

    public float GetHeight()
    {
        if (_localAvatarScaler == null)
            return PlayerAvatarPoint.defaultAvatarHeight;

        if (!_localAvatarScaler.IsHeightAdjustedFromInitial())
            return PlayerSetup.Instance.GetAvatarHeight();
        
        return _localAvatarScaler.GetHeight();
    }
    
    public float GetHeightForNetwork()
    {
        if (!_settingUniversalScaling)
            return -1f;
        
        if (_localAvatarScaler == null)
            return -1f;
        
        if (!_localAvatarScaler.IsHeightAdjustedFromInitial())
            return -1f;

        return _localAvatarScaler.GetHeight();
    }

    public float GetInitialHeight()
    {
        if (_localAvatarScaler == null)
            return -1f;

        return _localAvatarScaler.GetInitialHeight();
    }

    public bool IsHeightAdjustedFromInitial()
    {
        return _localAvatarScaler != null && _localAvatarScaler.IsHeightAdjustedFromInitial();
    }

    #endregion

    #region Network Methods

    public float GetNetworkHeight(string playerId)
    {
        if (_networkedScalers.TryGetValue(playerId, out NetworkScaler scaler))
            if (scaler.IsHeightAdjustedFromInitial()) return scaler.GetHeight();
        
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
}