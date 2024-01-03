using System.Diagnostics;
using ABI_RC.Core;
using NAK.AvatarScaleMod.AvatarScaling;
using NAK.AvatarScaleMod.ScaledComponents;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod.Components;

[DefaultExecutionOrder(-99999)] // before playersetup/puppetmaster but after animator
public class BaseScaler : MonoBehaviour
{
    #region Constants

    protected const string ScaleFactorParameterName = "ScaleFactor";
    protected const string ScaleFactorParameterNameLocal = "#" + ScaleFactorParameterName;

    #endregion

    #region Events

    // OnAnimatedHeightChanged
    public delegate void AnimatedHeightChangedDelegate(BaseScaler scaler);
    public event AnimatedHeightChangedDelegate OnAnimatedHeightChanged;
    
    // OnAnimatedHeightOverride
    public delegate void AnimatedHeightOverrideDelegate(BaseScaler scaler);
    public event AnimatedHeightOverrideDelegate OnAnimatedHeightOverride;
    
    // OnTargetHeightChanged
    public delegate void TargetHeightChangedDelegate(BaseScaler scaler);
    public event TargetHeightChangedDelegate OnTargetHeightChanged;
    
    // OnHeightReset
    public delegate void HeightResetDelegate(BaseScaler scaler);
    public event HeightResetDelegate OnTargetHeightReset;
    
    // ------------------------------------------------
    
    protected void InvokeAnimatedHeightChanged()
        => OnAnimatedHeightChanged?.Invoke(this);
    
    protected void InvokeAnimatedHeightOverride()
        => OnAnimatedHeightOverride?.Invoke(this);
    
    protected void InvokeTargetHeightChanged()
        => OnTargetHeightChanged?.Invoke(this);

    protected void InvokeTargetHeightReset()
        => OnTargetHeightReset?.Invoke(this);

    #endregion
    
    #region Variables

    private float _targetHeight;
    public float TargetHeight
    {
        get => _targetHeight;
        set
        {
            if (value < float.Epsilon)
            {
                // reset to animated height
                _targetHeight = _animatedHeight;
                _targetScale = _animatedScale;
                _scaleFactor = _animatedScaleFactor;
                _targetHeightChanged = true;
                return;
            }
            
            _targetHeight = Mathf.Clamp(value, AvatarScaleManager.MinHeight, AvatarScaleManager.MaxHeight);
            _scaleFactor = Mathf.Max(_targetHeight / _initialHeight, 0.01f); //safety
            _targetScale = _initialScale * _scaleFactor;
            _targetHeightChanged = true;
        }
    }

    protected bool _useTargetHeight;
    public bool UseTargetHeight
    {
        get => _useTargetHeight;
        set
        {
            if (_useTargetHeight == value)
                return;
            
            _useTargetHeight = value;
            _targetHeightChanged = true;
        }
    }

    // Config variables
    public bool avatarIsHidden { get; set; }
    public bool overrideAnimationHeight { get; set; }
    
    // State variables
    internal bool _isAvatarInstantiated;
    private bool _shouldForceHeight => _useTargetHeight || avatarIsHidden;
    private bool _targetHeightChanged;

    // Avatar info
    internal Transform _avatarTransform;
    internal CVRAnimatorManager _animatorManager;
    
    // Initial scaling
    internal float _initialHeight;
    internal Vector3 _initialScale;

    // Forced scaling (Universal & Hidden Avatar)
    internal Vector3 _targetScale = Vector3.one;
    internal float _scaleFactor = 1f;

    // AnimationClip-based scaling (Local Avatar)
    internal float _animatedHeight;
    internal Vector3 _animatedScale;
    internal float _animatedScaleFactor = 1f;

    #endregion

    #region Avatar Events
    
    public virtual void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (_isAvatarInstantiated) return;
        _isAvatarInstantiated = true;
        
        _initialHeight = _animatedHeight = Mathf.Clamp(initialHeight, 0.01f, 100f);
        _initialScale = _animatedScale = initialScale;
        _animatedScaleFactor = 1f;
        
        if (!_shouldForceHeight) // not universal or hidden avatar
        {
            _targetHeight = _initialHeight;
            _targetScale = _initialScale;
            _scaleFactor = 1f;
        }
        
        _avatarTransform = avatarObject.transform;
        
        Stopwatch stopwatch = new();
        stopwatch.Start();
        
        FindComponentsOfType(scalableComponentTypes);
        
        stopwatch.Stop();
        if (ModSettings.Debug_ComponentSearchTime.Value)
            AvatarScaleMod.Logger.Msg($"({typeof(LocalScaler)}) Component search time for {avatarObject}: {stopwatch.ElapsedMilliseconds}ms");        
    }

    public void OnAvatarDestroyed()
    {
        if (!_isAvatarInstantiated) return;
        _isAvatarInstantiated = false;

        _avatarTransform = null;
        ClearComponentLists();
    }

    #endregion

    #region Public Methods
    
    public float GetInitialHeight() => _initialHeight;
    public float GetTargetHeight() => _targetHeight;
    public float GetAnimatedHeight() => _animatedHeight;
    public bool IsForcingHeight() => _shouldForceHeight;

    #endregion

    #region Private Methods
    
    public bool ApplyTargetHeight()
    {
        if (!_isAvatarInstantiated || _initialHeight == 0)
            return false;

        if (_avatarTransform == null)
            return false;
        
        ScaleAvatarRoot();
        UpdateAnimatorParameter();
        ApplyComponentScaling();
        return true;
    }
    
    protected void ResetTargetHeight()
    {
        if (!_isAvatarInstantiated)
            return;

        if (_avatarTransform == null)
            return;
        
        _targetHeight = _initialHeight;
        _targetScale = _initialScale;
        _scaleFactor = 1f;
        _targetHeightChanged = true;
        
        ScaleAvatarRoot();
        UpdateAnimatorParameter();
        ApplyComponentScaling();
    }

    private void ScaleAvatarRoot()
    {
        if (_avatarTransform == null) return;
        _avatarTransform.localScale = _targetScale;
    }

    protected virtual void UpdateAnimatorParameter()
    {
        // empty
    }

    #endregion

    #region Unity Events

    public virtual void LateUpdate()
    {
        if (!_isAvatarInstantiated)
            return; // no avatar
        
        if (!_targetHeightChanged 
            && _useTargetHeight)
            ScaleAvatarRoot();

        if (!_targetHeightChanged) 
            return;
        
        if (_useTargetHeight)
            ApplyTargetHeight();
        else
            ResetTargetHeight();
    }

    internal virtual void OnDestroy()
    {
        ClearComponentLists();
    }

    #endregion

    #region Component Scaling

    internal static readonly Type[] scalableComponentTypes =
    {
        typeof(Light),
        typeof(AudioSource),
        typeof(ParticleSystem),
        typeof(ParentConstraint),
        typeof(PositionConstraint),
        typeof(ScaleConstraint)
    };

    private readonly List<ScaledLight> _scaledLights = new();
    private readonly List<ScaledAudioSource> _scaledAudioSources = new();
    private readonly List<ScaledParentConstraint> _scaledParentConstraints = new();
    private readonly List<ScaledPositionConstraint> _scaledPositionConstraints = new();
    private readonly List<ScaledScaleConstraint> _scaledScaleConstraints = new();

    private void ClearComponentLists()
    {
        _scaledLights.Clear();
        _scaledAudioSources.Clear();
        _scaledParentConstraints.Clear();
        _scaledPositionConstraints.Clear();
        _scaledScaleConstraints.Clear();
    }

    internal void FindComponentsOfType(Type[] types)
    {
        var components = _avatarTransform.gameObject.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (this == null) break;
            if (component == null) continue;

            Type componentType = component.GetType();
            if (types.Contains(componentType))
                AddScaledComponent(componentType, component);
        }
    }
    
    private void AddScaledComponent(Type type, Component component)
    {
        switch (type)
        {
            case not null when type == typeof(AudioSource):
                _scaledAudioSources.Add(new ScaledAudioSource((AudioSource)component));
                break;
            case not null when type == typeof(Light):
                _scaledLights.Add(new ScaledLight((Light)component));
                break;
            case not null when type == typeof(ParentConstraint):
                _scaledParentConstraints.Add(new ScaledParentConstraint((ParentConstraint)component));
                break;
            case not null when type == typeof(PositionConstraint):
                _scaledPositionConstraints.Add(new ScaledPositionConstraint((PositionConstraint)component));
                break;
            case not null when type == typeof(ScaleConstraint):
                _scaledScaleConstraints.Add(new ScaledScaleConstraint((ScaleConstraint)component));
                break;
        }
    }

    private void ApplyComponentScaling()
    {
        // UpdateLightScales(); // might break dps
        UpdateAudioSourceScales();
        UpdateParentConstraintScales();
        UpdatePositionConstraintScales();
        UpdateScaleConstraintScales();
    }

    private void UpdateLightScales()
    {
        // Update range of each light component
        foreach (ScaledLight light in _scaledLights)
            light.Scale(_scaleFactor);
    }

    private void UpdateAudioSourceScales()
    {
        // Update min and max distance of each audio source component
        foreach (ScaledAudioSource audioSource in _scaledAudioSources)
            audioSource.Scale(_scaleFactor);
    }

    private void UpdateParentConstraintScales()
    {
        // Update translationAtRest and translationOffsets of each parent constraint component
        foreach (ScaledParentConstraint parentConstraint in _scaledParentConstraints)
            parentConstraint.Scale(_scaleFactor);
    }

    private void UpdatePositionConstraintScales()
    {
        // Update translationAtRest and translationOffset of each position constraint component
        foreach (ScaledPositionConstraint positionConstraint in _scaledPositionConstraints)
            positionConstraint.Scale(_scaleFactor);
    }

    private void UpdateScaleConstraintScales()
    {
        // Update scaleAtRest and scaleOffset of each scale constraint component
        foreach (ScaledScaleConstraint scaleConstraint in _scaledScaleConstraints)
            scaleConstraint.Scale(_scaleFactor);
    }

    #endregion
}