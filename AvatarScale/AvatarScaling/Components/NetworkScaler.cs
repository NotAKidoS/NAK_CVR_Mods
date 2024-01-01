using System.Diagnostics;
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
        
        heightNeedsUpdate = false;
        _isAvatarInstantiated = false;
    }

    #endregion

    #region Overrides

    public override void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (avatarObject == null)
            return;
        
        base.OnAvatarInstantiated(avatarObject, initialHeight, initialScale);
        
        Stopwatch stopwatch = new();
        stopwatch.Start();
        FindComponentsOfType(scalableComponentTypes);
        stopwatch.Stop();
        if (ModSettings.Debug_ComponentSearchTime.Value)
            AvatarScaleMod.Logger.Msg($"({typeof(NetworkScaler)}) Component search time for {avatarObject}: {stopwatch.ElapsedMilliseconds}ms");

        // TODO: why did i do this? height is never set prior to this method being called
        // if (_isHeightAdjustedFromInitial && heightNeedsUpdate) 
        //     UpdateScaleIfInstantiated();
    }

    protected override void UpdateAnimatorParameter()
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