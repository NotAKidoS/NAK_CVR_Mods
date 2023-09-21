using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.AvatarTracking;
using ABI_RC.Systems.GameEventSystem;
using NAK.AvatarScaleMod.Networking;
using UnityEngine;

namespace NAK.AvatarScaleMod.AvatarScaling;

public class AvatarScaleManager : MonoBehaviour
{
    public static AvatarScaleManager Instance;

    public bool Setting_PersistantHeight = false;

    private Dictionary<string, UniversalAvatarScaler> _networkedScalers;
    private UniversalAvatarScaler _localAvatarScaler;

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;
        _networkedScalers = new Dictionary<string, UniversalAvatarScaler>();
    }

    private void Start()
    {
        CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);
        //SchedulerSystem.AddJob(new SchedulerSystem.Job(ForceHeightUpdate), 0f, 10f, -1);
    }

    private void OnDestroy()
    {
        CVRGameEventSystem.Instance.OnConnected.RemoveListener(OnInstanceConnected);
        //SchedulerSystem.RemoveJob(new SchedulerSystem.Job(ForceHeightUpdate));
    }

    #endregion

    #region Game Events

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
            _localAvatarScaler = playerSetup.gameObject.AddComponent<UniversalAvatarScaler>();
            _localAvatarScaler.Initialize();
        }

        _localAvatarScaler.OnAvatarInstantiated(playerSetup._avatar, playerSetup._initialAvatarHeight,
            playerSetup.initialScale);
        
        if (Setting_PersistantHeight && _localAvatarScaler.IsValid())
            SchedulerSystem.AddJob(() => { ModNetwork.SendNetworkHeight(_localAvatarScaler.GetHeight()); }, 0.5f, 0f, 1);
    }

    public void OnAvatarDestroyed(PlayerSetup playerSetup)
    {
        if (_localAvatarScaler != null)
            _localAvatarScaler.OnAvatarDestroyed(Setting_PersistantHeight);
        
        if (Setting_PersistantHeight && _localAvatarScaler.IsValid())
            SchedulerSystem.AddJob(() => { ModNetwork.SendNetworkHeight(_localAvatarScaler.GetHeight()); }, 0.5f, 0f, 1);
    }

    public void SetHeight(float targetHeight)
    {
        if (_localAvatarScaler == null)
            return;

        _localAvatarScaler.SetTargetHeight(targetHeight);
        ModNetwork.SendNetworkHeight(targetHeight);

        // immediately update play space scale
        PlayerSetup.Instance.CheckUpdateAvatarScaleToPlaySpaceRelation();
    }

    public void ResetHeight()
    {
        if (_localAvatarScaler != null)
            _localAvatarScaler.ResetHeight();
    }

    public float GetHeight()
    {
        return _localAvatarScaler != null ? _localAvatarScaler.GetHeight() : -1f;
    }

    #endregion

    #region Network Methods

    public float GetNetworkHeight(string playerId)
    {
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
            return scaler.GetHeight();
        return -1f;
    }

    // we will create a Universal Scaler for only users that send a height update
    // this is sent at a rate of 10s locally!

    internal void OnNetworkHeightUpdateReceived(string playerId, float targetHeight)
    {
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
            scaler.SetTargetHeight(targetHeight);
        else
            SetupHeightScalerForNetwork(playerId, targetHeight);
    }

    internal void OnNetworkAvatarInstantiated(PuppetMaster puppetMaster)
    {
        var playerId = puppetMaster._playerDescriptor.ownerId;
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
            scaler.OnAvatarInstantiated(puppetMaster.avatarObject, puppetMaster._initialAvatarHeight,
                puppetMaster.initialAvatarScale);
    }

    internal void OnNetworkAvatarDestroyed(PuppetMaster puppetMaster)
    {
        // on disconnect
        if (puppetMaster == null || puppetMaster._playerDescriptor == null)
            return;

        var playerId = puppetMaster._playerDescriptor.ownerId;
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
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

        UniversalAvatarScaler scaler = puppetMaster.gameObject.AddComponent<UniversalAvatarScaler>();
        scaler.Initialize(playerId);

        scaler.OnAvatarInstantiated(puppetMaster.avatarObject, puppetMaster._initialAvatarHeight,
            puppetMaster.initialAvatarScale);

        _networkedScalers[playerId] = scaler;

        scaler.SetTargetHeight(targetHeight); // set initial height
    }

    #endregion
}