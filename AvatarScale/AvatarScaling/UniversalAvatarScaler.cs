using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using NAK.AvatarScaleMod.ScaledComponents;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod.AvatarScaling;

[DefaultExecutionOrder(-99999)] // before playersetup/puppetmaster but after animator
public class UniversalAvatarScaler : MonoBehaviour
{
    #region Constants

    // Universal Scaling Limits
    private const float MinHeight = 0.1f;
    private const float MaxHeight = 10f;
    
    private const string ScaleFactorParameterName = "ScaleFactor";
    private const string ScaleFactorParameterNameLocal = "#ScaleFactor";

    #endregion

    #region Variables

    internal bool requestedInitial;
    
    [NonSerialized]
    internal string ownerId;

    private Transform _avatarTransform;
    private CVRAnimatorManager _animatorManager;

    private float _initialHeight;
    private Vector3 _initialScale;

    private float _targetHeight;
    private float _scaleFactor = 1f;

    private bool _isLocalAvatar;
    private bool _heightWasUpdated;

    private bool _isAvatarInstantiated;
    
    #endregion

    #region Unity Methods

    private void LateUpdate()
    {
        ScaleAvatarRoot(); // override animation-based scaling
    }

    private void OnDestroy()
    {
        ClearComponentLists();
        if (!_isLocalAvatar) AvatarScaleManager.Instance.RemoveNetworkHeightScaler(ownerId);
    }

    #endregion

    #region Public Methods

    public void Initialize(string playerId = null)
    {
        ownerId = playerId;
        
        _isLocalAvatar = gameObject.layer == 8;
        
        _animatorManager = _isLocalAvatar
            ? GetComponentInParent<PlayerSetup>().animatorManager
            : GetComponentInParent<PuppetMaster>()._animatorManager;
        
        _heightWasUpdated = false;
        _isAvatarInstantiated = false;
    }

    public async void OnAvatarInstantiated(GameObject avatarObject, float initialHeight, Vector3 initialScale)
    {
        if (avatarObject == null)
        {
            AvatarScaleMod.Logger.Error("Avatar was somehow null?????");
            return;
        }
        AvatarScaleMod.Logger.Msg($"Avatar Object : {_avatarTransform} : {_avatarTransform == null}");
        
        if (_isAvatarInstantiated) return;
        _isAvatarInstantiated = true;
        
        // if we don't have a queued height update, apply initial scaling
        if (!_heightWasUpdated) 
            _targetHeight = initialHeight;
        
        _initialHeight = initialHeight;
        _initialScale = initialScale;
        _scaleFactor = _targetHeight / _initialHeight;
        
        _avatarTransform = avatarObject.transform;
        await FindComponentsOfTypeAsync(scalableComponentTypes);

        ApplyScaling(); // apply queued scaling if avatar was loading
    }

    public void OnAvatarDestroyed(bool shouldPersist = false)
    {
        if (!_isAvatarInstantiated) return;
        _isAvatarInstantiated = false;
        
        AvatarScaleMod.Logger.Msg($"Destroying Avatar Object : {_avatarTransform} : {_avatarTransform == null}");
        
        _avatarTransform = null;
        _heightWasUpdated = shouldPersist;
        ClearComponentLists();
    }

    public void SetTargetHeight(float height)
    {
        if (Math.Abs(height - _targetHeight) < float.Epsilon)
            return;
        
        _targetHeight = Mathf.Clamp(height, MinHeight, MaxHeight);
        
        _heightWasUpdated = true;
        if (!_isAvatarInstantiated) 
            return;
        
        _scaleFactor = _targetHeight / _initialHeight;
        ApplyScaling();
    }
    
    public void ResetHeight()
    {
        if (Math.Abs(_initialHeight - _targetHeight) < float.Epsilon)
            return;
        
        _targetHeight = _initialHeight;
        
        _heightWasUpdated = true;
        if (!_isAvatarInstantiated) 
            return;
        
        _scaleFactor = 1f;
        ApplyScaling();
    }

    public float GetHeight()
    {
        return _targetHeight;
    }

    public bool IsValid()
    {
        return _isAvatarInstantiated;
    }

    #endregion

    #region Private Methods

    private void ScaleAvatarRoot()
    {
        if (_avatarTransform == null)
            return;
        
        _avatarTransform.localScale = _initialScale * _scaleFactor;
    }
    
    private void UpdateAnimatorParameter()
    {
        if (_animatorManager == null) 
            return;
        
        // synced parameter
        if (_isLocalAvatar) _animatorManager.SetAnimatorParameter(ScaleFactorParameterName, _scaleFactor);
        // local parameter
        _animatorManager.SetAnimatorParameter(ScaleFactorParameterNameLocal, _scaleFactor);
    }

    private void ApplyScaling()
    {
        if (_avatarTransform == null)
            return;
        
        _heightWasUpdated = false;
        
        ScaleAvatarRoot();
        UpdateAnimatorParameter();
        ApplyComponentScaling();
    }

    #endregion

    #region Component Scaling

    private static readonly Type[] scalableComponentTypes =
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
    
    private async Task FindComponentsOfTypeAsync(Type[] types)
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