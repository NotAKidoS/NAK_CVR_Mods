using ABI_RC.Core.Player;
using NAK.AvatarScaleMod.Networking;
using UnityEngine;

namespace NAK.AvatarScaleMod.AvatarScaling;

public class AvatarScaleManager : MonoBehaviour
{
    public static AvatarScaleManager Instance;

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

    #endregion

    #region Local Methods

    public void OnAvatarInstantiated(PlayerSetup playerSetup)
    {
        if (playerSetup._avatar == null)
            return;
        
        if (_localAvatarScaler != null)
            Destroy(_localAvatarScaler);

        _localAvatarScaler = playerSetup._avatar.AddComponent<UniversalAvatarScaler>();
        _localAvatarScaler.Initialize(playerSetup._initialAvatarHeight, playerSetup.initialScale);
    }

    public void OnAvatarDestroyed()
    {
        if (_localAvatarScaler != null)
            Destroy(_localAvatarScaler);
    }

    public void SetHeight(float targetHeight)
    {
        if (_localAvatarScaler == null) 
            return;
        
        _localAvatarScaler.SetHeight(targetHeight);
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
        return (_localAvatarScaler != null) ? _localAvatarScaler.GetHeight() : -1f;
    }

    #endregion

    #region Network Methods

    public void OnNetworkAvatarInstantiated(PuppetMaster puppetMaster)
    {
        if (puppetMaster.avatarObject == null)
            return; 
        
        string playerId = puppetMaster._playerDescriptor.ownerId;
        
        if (_networkedScalers.ContainsKey(playerId))
            _networkedScalers.Remove(playerId);

        UniversalAvatarScaler scaler = puppetMaster.avatarObject.AddComponent<UniversalAvatarScaler>();
        scaler.Initialize(puppetMaster._initialAvatarHeight, puppetMaster.initialAvatarScale);
        _networkedScalers[playerId] = scaler;
    }

    public void OnNetworkAvatarDestroyed(string playerId)
    {
        if (_networkedScalers.ContainsKey(playerId))
            _networkedScalers.Remove(playerId);
    }

    public void OnNetworkHeightUpdateReceived(string playerId, float targetHeight)
    {
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
            scaler.SetHeight(targetHeight);
    }
    
    public float GetNetworkHeight(string playerId)
    {
        if (_networkedScalers.TryGetValue(playerId, out UniversalAvatarScaler scaler))
            return scaler.GetHeight();
        return -1f;
    }
    
    #endregion
}