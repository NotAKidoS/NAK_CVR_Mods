using ABI_RC.Core.Player;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Components;

public class NetworkScaler : BaseScaler
{
    private string playerGuid;
    
    #region Public Methods
    
    public void Initialize(string playerId)
    {
        playerGuid = playerId;
        
        _animatorManager = GetComponentInParent<PuppetMaster>().animatorManager;
        
        _heightNeedsUpdate = false;
        _isAvatarInstantiated = false;
        _isHeightAdjustedFromInitial = false;
    }

    #endregion

    #region Overrides

    public override async void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (avatarObject == null)
            return;
        
        base.OnAvatarInstantiated(avatarObject, initialHeight, initialScale);
        await FindComponentsOfTypeAsync(scalableComponentTypes);

        if (_isHeightAdjustedFromInitial && _heightNeedsUpdate)
            UpdateScaleIfInstantiated();
    }
    
    internal override void UpdateAnimatorParameter()
    {
        _animatorManager?.SetAnimatorParameter(ScaleFactorParameterNameLocal, _scaleFactor);
    }
    
    internal override void OnDestroy()
    {
        AvatarScaleManager.Instance.RemoveNetworkHeightScaler(playerGuid);
        base.OnDestroy();
    }

    #endregion
}