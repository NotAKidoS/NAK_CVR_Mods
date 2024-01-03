using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Components;

public class LocalScaler : BaseScaler
{
    #region Public Methods
    
    public void Initialize()
    {
        _animatorManager = GetComponentInParent<PlayerSetup>().animatorManager;
        _isAvatarInstantiated = false;
    }
    
    #endregion

    #region Overrides

    public override void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (avatarObject == null)
            return;

        base.OnAvatarInstantiated(avatarObject, initialHeight, initialScale);
    }

    protected override void UpdateAnimatorParameter()
    {
        if (_animatorManager == null) 
            return;

        _animatorManager.SetAnimatorParameter(ScaleFactorParameterName, _scaleFactor);
        _animatorManager.SetAnimatorParameter(ScaleFactorParameterNameLocal, _scaleFactor);
    }
    
    public override void LateUpdate()
    {
        if (!CheckForAnimationScaleChange())
            base.LateUpdate();
    }

    #endregion
    
    #region Private Methods
    
    private bool CheckForAnimationScaleChange()
    {
        if (_avatarTransform == null) 
            return false;
        
        Vector3 localScale = _avatarTransform.localScale;
        
        // scale matches last recorded animation scale
        if (localScale == _animatedScale) 
            return false;
        
        // avatar may not have scale animation, check if it isn't equal to targetScale
        if (localScale == _targetScale)
            return false;
        
        // this is the first time we've seen the avatar animated scale, record it!
        if (_animatedScale == Vector3.zero)
        {
            _animatedScale = localScale;
            return false;
        }
        
        // animation scale changed, record it!
        Vector3 scaleDifference = PlayerSetup.DivideVectors(localScale - _initialScale, _initialScale);
        _animatedScaleFactor = scaleDifference.y;
        _animatedHeight = (_initialHeight * _animatedScaleFactor) + _initialHeight;
        _animatedScale = localScale;
        InvokeAnimatedHeightChanged();
        
        if (overrideAnimationHeight 
            || !_useTargetHeight)
            return false; // user has disabled animation height override or is not using universal scaling
        
        // animation scale changed and now will override universal scaling
        ResetTargetHeight();
        InvokeAnimatedHeightOverride();
        
        return true;
    }
    
    #endregion
}