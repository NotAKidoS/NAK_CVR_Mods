using ABI_RC.Core.Player;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Components;

public class LocalScaler : BaseScaler
{
    #region Public Methods
    
    public void Initialize()
    {
        _animatorManager = GetComponentInParent<PlayerSetup>().animatorManager;
        
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
        
        _targetHeight = initialHeight;
        _scaleFactor = 1f;
        
        _legacyAnimationScale = Vector3.zero;
    }
    
    internal override void UpdateAnimatorParameter()
    {
        if (_animatorManager == null) 
            return;

        _animatorManager.SetAnimatorParameter(ScaleFactorParameterName, _scaleFactor);
        _animatorManager.SetAnimatorParameter(ScaleFactorParameterNameLocal, _scaleFactor);
    }
    
    public override void LateUpdate()
    {
        if (!_isHeightAdjustedFromInitial)
            return;
        
        if (!_isAvatarInstantiated) 
            return;

        if (!CheckForAnimationScaleChange())
            ScaleAvatarRoot();
    }

    #endregion
    
    #region Private Methods
    
    private bool CheckForAnimationScaleChange()
    {
        if (_avatarTransform == null) return false;
        if (_avatarTransform.localScale == _legacyAnimationScale) 
            return false;
        
        // scale was likely reset or not initiated
        if (_legacyAnimationScale == Vector3.zero)
        {
            _legacyAnimationScale = _avatarTransform.localScale;
            return false;
        }
        
        _legacyAnimationScale = _avatarTransform.localScale;
        
        AvatarScaleMod.Logger.Msg("AnimationClip-based avatar scaling detected. Disabling Universal Scaling.");
        AvatarScaleManager.Instance.ResetHeight(); // disable mod, user used a scale slider
        return true;
    }

    #endregion
}