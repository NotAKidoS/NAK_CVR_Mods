using ABI_RC.Core;
using ABI_RC.Core.Player;
using NAK.AvatarScaleMod.AvatarScaling;
using NAK.AvatarScaleMod.ScaledComponents;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod.Components;

[DefaultExecutionOrder(-99999)] // before playersetup/puppetmaster but after animator
public class BaseScaler : MonoBehaviour
{
    #region Constants

    public const string ScaleFactorParameterName = "ScaleFactor";
    public const string ScaleFactorParameterNameLocal = "#ScaleFactor";

    #endregion

    #region Variables
    
    internal bool _isAvatarInstantiated;
    internal bool _isHeightAdjustedFromInitial;
    internal bool _heightNeedsUpdate;
    
    internal Transform _avatarTransform;
    internal CVRAnimatorManager _animatorManager;

    internal float _initialHeight;
    internal Vector3 _initialScale;

    internal float _targetHeight = -1;
    internal Vector3 _targetScale = Vector3.one;
    internal float _scaleFactor = 1f;
    
    // detection for animation clip-based scaling
    internal Vector3 _legacyAnimationScale;
    
    #endregion
    
    #region Public Methods

    public virtual void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (_isAvatarInstantiated) return;
        _isAvatarInstantiated = true;
        
        _initialHeight = Mathf.Clamp(initialHeight, 0.01f, 100f);
        _initialScale = initialScale;
        _avatarTransform = avatarObject.transform;
    }

    public void OnAvatarDestroyed()
    {
        if (!_isAvatarInstantiated) return;
        _isAvatarInstantiated = false;
        
        _avatarTransform = null;
        _heightNeedsUpdate = false;
        ClearComponentLists();
    }

    public void SetTargetHeight(float height)
    {
        if (Math.Abs(height - _targetHeight) < float.Epsilon)
            return;
        
        if (height < float.Epsilon)
        {
            ResetHeight();
            return;
        }
        
        if (!_isHeightAdjustedFromInitial)
            _legacyAnimationScale = Vector3.zero;
        
        _isHeightAdjustedFromInitial = true;
        
        _targetHeight = Mathf.Clamp(height, AvatarScaleManager.MinHeight, AvatarScaleManager.MaxHeight);
        _heightNeedsUpdate = true;

        UpdateScaleIfInstantiated();
    }

    public void ResetHeight()
    {
        if (!_isHeightAdjustedFromInitial) return;
        _isHeightAdjustedFromInitial = false;

        if (Math.Abs(_initialHeight - _targetHeight) < float.Epsilon)
            return;

        _legacyAnimationScale = Vector3.zero;
        
        _targetHeight = _initialHeight;
        _heightNeedsUpdate = true;

        UpdateScaleIfInstantiated();
    }

    public float GetHeight() => _targetHeight;
    public float GetInitialHeight() => _initialHeight;
    public bool IsHeightAdjustedFromInitial() => _isHeightAdjustedFromInitial;

    #endregion
    
    #region Private Methods

    internal void ScaleAvatarRoot()
    {
        if (_avatarTransform == null) return;
        _avatarTransform.localScale = _targetScale;
    }
    
    internal virtual void UpdateAnimatorParameter()
    {
        // empty
    }
    
    internal void UpdateScaleIfInstantiated()
    {
        if (!_isAvatarInstantiated || _initialHeight == 0) 
            return;
        
        if (_avatarTransform == null) 
            return;

        _scaleFactor = Mathf.Max(_targetHeight / _initialHeight, 0.01f); //safety
        
        _heightNeedsUpdate = false;
        _targetScale = _initialScale * _scaleFactor;

        ScaleAvatarRoot();
        UpdateAnimatorParameter();
        ApplyComponentScaling();
    }

    #endregion
    
    #region Unity Methods

    public virtual void LateUpdate()
    {
        if (!_isHeightAdjustedFromInitial)
            return;
        
        if (!_isAvatarInstantiated) 
            return;
        
        ScaleAvatarRoot(); // override animationclip-based scaling
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
    
    private readonly List<ScaledLight> _scaledLights = new List<ScaledLight>();
    private readonly List<ScaledAudioSource> _scaledAudioSources = new List<ScaledAudioSource>();
    private readonly List<ScaledParentConstraint> _scaledParentConstraints = new List<ScaledParentConstraint>();
    private readonly List<ScaledPositionConstraint> _scaledPositionConstraints = new List<ScaledPositionConstraint>();
    private readonly List<ScaledScaleConstraint> _scaledScaleConstraints = new List<ScaledScaleConstraint>();
    
    private void ClearComponentLists()
    {
        _scaledLights.Clear();
        _scaledAudioSources.Clear();
        _scaledParentConstraints.Clear();
        _scaledPositionConstraints.Clear();
        _scaledScaleConstraints.Clear();
    }
    
    internal async Task FindComponentsOfTypeAsync(Type[] types)
    {
        var tasks = new List<Task>();
        var components = _avatarTransform.gameObject.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (this == null) break;
            if (component == null) continue;

            tasks.Add(Task.Run(() =>
            {
                Type componentType = component.GetType();
                if (types.Contains(componentType))
                {
                    AddScaledComponent(componentType, component);
                }
            }));
        }

        await Task.WhenAll(tasks);
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