using ABI_RC.Core;
using ABI_RC.Core.Player;
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

    private CVRAnimatorManager _animatorManager;

    private float _initialHeight;
    private Vector3 _initialScale;

    private float _targetHeight;
    private float _scaleFactor = 1f;

    private bool _isLocalAvatar;
    private bool _heightWasUpdated;

    #endregion

    #region Unity Methods

    private async void Start()
    {
        await FindComponentsOfTypeAsync(scalableComponentTypes);
    }

    private void LateUpdate()
    {
        ScaleAvatarRoot(); // override animation-based scaling
    }

    #endregion

    #region Public Methods

    public void Initialize(float initialHeight, Vector3 initialScale)
    {
        _initialHeight = _targetHeight = initialHeight;
        _initialScale = initialScale;
        _scaleFactor = 1f;

        _isLocalAvatar = gameObject.layer == 8;
        
        _animatorManager = _isLocalAvatar
            ? GetComponentInParent<PlayerSetup>().animatorManager
            : GetComponentInParent<PuppetMaster>()._animatorManager;

        _heightWasUpdated = false;
    }

    public void SetHeight(float height)
    {
        _targetHeight = Mathf.Clamp(height, MinHeight, MaxHeight);
        _scaleFactor = _targetHeight / _initialHeight;
        _heightWasUpdated = true;
        ApplyScaling();
    }
    
    public void ResetHeight()
    {
        _targetHeight = _initialHeight;
        _scaleFactor = 1f;
        _heightWasUpdated = true;
        ApplyScaling();
    }

    public float GetHeight()
    {
        return _targetHeight;
    }

    #endregion

    #region Private Methods

    private void ScaleAvatarRoot()
    {
        transform.localScale = _initialScale * _scaleFactor;
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
        if (!_heightWasUpdated)
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

    private async Task FindComponentsOfTypeAsync(Type[] types)
    {
        var tasks = new List<Task>();
        var components = GetComponentsInChildren<Component>(true);

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