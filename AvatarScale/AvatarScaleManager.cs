using ABI.CCK.Components;
using ABI_RC.Core.Player;
using NAK.AvatarScaleMod.ScaledComponents;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod;

public class AvatarScaleManager : MonoBehaviour
{
    // Constants
    public const float MinHeight = 0.1f; // TODO: Make into Setting
    public const float MaxHeight = 10f; // TODO: Make into Setting
    public const string ScaleFactorParameterName = "ScaleFactor";
    public const string ScaleFactorParameterNameLocal = "#ScaleFactor";

    private static readonly System.Type[] scalableComponentTypes =
    {
        typeof(Light),
        typeof(AudioSource),
        typeof(ParticleSystem),
        typeof(ParentConstraint),
        typeof(PositionConstraint),
        typeof(ScaleConstraint)
    };

    // Public properties
    public static bool GlobalEnabled { get; set; }
    public static AvatarScaleManager LocalAvatar { get; private set; }

    public float TargetHeight { get; private set; }
    public float InitialHeight { get; private set; }
    public Vector3 InitialScale { get; private set; }
    public float ScaleFactor { get; private set; }

    // Private properties
    private bool _isLocalAvatar;
    private Animator _animator;
    private CVRAvatar _avatar;

    private List<ScaledLight> _scaledLights = new List<ScaledLight>();
    private List<ScaledAudioSource> _scaledAudioSources = new List<ScaledAudioSource>();
    private List<ScaledParentConstraint> _scaledParentConstraints = new List<ScaledParentConstraint>();
    private List<ScaledPositionConstraint> _scaledPositionConstraints = new List<ScaledPositionConstraint>();
    private List<ScaledScaleConstraint> _scaledScaleConstraints = new List<ScaledScaleConstraint>();

    public void Initialize(float initialHeight, Vector3 initialScale, bool isLocalAvatar)
    {
        if (Math.Abs(initialHeight) < 1E-6)
        {
            AvatarScaleMod.Logger.Warning("Cannot initialize with a height of zero!");
            return;
        }

        if (isLocalAvatar && LocalAvatar == null)
        {
            _isLocalAvatar = true;
            LocalAvatar = this;
        }

        this.TargetHeight = initialHeight;
        this.InitialHeight = initialHeight;
        this.InitialScale = initialScale;
        this.ScaleFactor = 1f;
    }

    private async void Start()
    {
        _avatar = GetComponent<CVRAvatar>();

        if (_avatar == null)
        {
            AvatarScaleMod.Logger.Error("AvatarScaleManager should be attached to a GameObject with a CVRAvatar component.");
            return;
        }

        if (!_isLocalAvatar)
        {
            _animator = GetComponent<Animator>();
        }

        // I am unsure if this reduces the hitch or not.
        // I do not want to patch where the game already does scanning though.
        await FindComponentsOfTypeAsync(scalableComponentTypes);
    }

    private void OnDisable()
    {
        // TODO: Test with Avatar Distance Hider
        ResetAllToInitialScale();
    }

    private void OnDestroy()
    {
        ClearLists();

        if (LocalAvatar == this)
        {
            LocalAvatar = null;
        }
    }

    private void ClearLists()
    {
        _scaledAudioSources.Clear();
        _scaledLights.Clear();
        _scaledParentConstraints.Clear();
        _scaledPositionConstraints.Clear();
        _scaledScaleConstraints.Clear();
    }

    public void SetTargetHeight(float newHeight)
    {
        TargetHeight = Mathf.Clamp(newHeight, MinHeight, MaxHeight);
        UpdateScaleFactor();
        UpdateAnimatorParameter();
    }

    public void SetTargetHeightOverTime(float newHeight, float duration)
    {
        StartCoroutine(SetTargetHeightOverTimeCoroutine(newHeight, duration));
    }

    private void UpdateScaleFactor()
    {
        if (Math.Abs(InitialHeight) < 1E-6)
        {
            AvatarScaleMod.Logger.Warning("InitialHeight is zero, cannot calculate ScaleFactor.");
            return;
        }

        ScaleFactor = TargetHeight / InitialHeight;
    }

    private void UpdateAnimatorParameter()
    {
        if (_isLocalAvatar)
        {
            // Set synced and local parameters for Local Player
            PlayerSetup.Instance.animatorManager.SetAnimatorParameter(ScaleFactorParameterName, ScaleFactor);
            PlayerSetup.Instance.animatorManager.SetAnimatorParameter(ScaleFactorParameterNameLocal, ScaleFactor);
        }
        else if (_animator != null)
        {
            // Set local parameter for Remote Player
            _animator.SetFloat(ScaleFactorParameterNameLocal, ScaleFactor);
        }
    }

    private Vector3 CalculateNewScale()
    {
        return InitialScale * ScaleFactor;
    }

    private IEnumerator SetTargetHeightOverTimeCoroutine(float newHeight, float duration)
    {
        float startTime = Time.time;
        float startHeight = TargetHeight;

        // Clamping the newHeight to be between MinHeight and MaxHeight
        newHeight = Mathf.Clamp(newHeight, MinHeight, MaxHeight);

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            TargetHeight = Mathf.Lerp(startHeight, newHeight, t);
            UpdateScaleFactor();
            yield return null;
        }

        // Final setting of the TargetHeight after the loop is done.
        TargetHeight = newHeight;
        UpdateScaleFactor();
    }

    // TODO: actually profile this
    private async Task FindComponentsOfTypeAsync(Type[] types)
    {
        var tasks = new List<Task>();
        var components = GetComponentsInChildren<Component>(true);

        foreach (var component in components)
        {
            if (this == null) break;
            if (component == null) continue;

            tasks.Add(Task.Run(() =>
            {
                var componentType = component.GetType();
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
            case Type _ when type == typeof(AudioSource):
                _scaledAudioSources.Add(new ScaledAudioSource((AudioSource)component));
                break;
            case Type _ when type == typeof(Light):
                _scaledLights.Add(new ScaledLight((Light)component));
                break;
            case Type _ when type == typeof(ParentConstraint):
                _scaledParentConstraints.Add(new ScaledParentConstraint((ParentConstraint)component));
                break;
            case Type _ when type == typeof(PositionConstraint):
                _scaledPositionConstraints.Add(new ScaledPositionConstraint((PositionConstraint)component));
                break;
            case Type _ when type == typeof(ScaleConstraint):
                _scaledScaleConstraints.Add(new ScaledScaleConstraint((ScaleConstraint)component));
                break;
        }
    }

    void Update()
    {
        ApplyAvatarScaling();
        ApplyComponentScaling();
    }

    void LateUpdate()
    {
        ApplyAvatarScaling();
        ApplyComponentScaling();
    }

    private void ApplyAvatarScaling()
    {
        if (!GlobalEnabled)
            return;

        transform.localScale = CalculateNewScale();
    }

    private void ApplyComponentScaling()
    {
        if (!GlobalEnabled)
            return;

        UpdateLightScales();
        UpdateAudioSourceScales();
        UpdateParentConstraintScales();
        UpdatePositionConstraintScales();
        UpdateScaleConstraintScales();
    }

    private void UpdateLightScales()
    {
        // Update range of each light component
        foreach (var light in _scaledLights)
        {
            light.Component.range = light.InitialRange * ScaleFactor;
        }
    }

    private void UpdateAudioSourceScales()
    {
        // Update min and max distance of each audio source component
        foreach (var audioSource in _scaledAudioSources)
        {
            audioSource.Component.minDistance = audioSource.InitialMinDistance * ScaleFactor;
            audioSource.Component.maxDistance = audioSource.InitialMaxDistance * ScaleFactor;
        }
    }

    private void UpdateParentConstraintScales()
    {
        // Update translationAtRest and translationOffsets of each parent constraint component
        foreach (var parentConstraint in _scaledParentConstraints)
        {
            parentConstraint.Component.translationAtRest = parentConstraint.InitialTranslationAtRest * ScaleFactor;

            for (int i = 0; i < parentConstraint.InitialTranslationOffsets.Count; i++)
            {
                parentConstraint.Component.translationOffsets[i] = parentConstraint.InitialTranslationOffsets[i] * ScaleFactor;
            }
        }
    }

    private void UpdatePositionConstraintScales()
    {
        // Update translationAtRest and translationOffset of each position constraint component
        foreach (var positionConstraint in _scaledPositionConstraints)
        {
            positionConstraint.Component.translationAtRest = positionConstraint.InitialTranslationAtRest * ScaleFactor;
            positionConstraint.Component.translationOffset = positionConstraint.InitialTranslationOffset * ScaleFactor;
        }
    }

    private void UpdateScaleConstraintScales()
    {
        // Update scaleAtRest and scaleOffset of each scale constraint component
        foreach (var scaleConstraint in _scaledScaleConstraints)
        {
            scaleConstraint.Component.scaleAtRest = scaleConstraint.InitialScaleAtRest * ScaleFactor;
            scaleConstraint.Component.scaleOffset = scaleConstraint.InitialScaleOffset * ScaleFactor;
        }
    }

    private void ResetAllToInitialScale()
    {
        // Reset transform scale and each component to their initial scales
        transform.localScale = InitialScale;

        foreach (var light in _scaledLights)
        {
            light.Component.range = light.InitialRange;
        }
        foreach (var audioSource in _scaledAudioSources)
        {
            audioSource.Component.minDistance = audioSource.InitialMinDistance;
            audioSource.Component.maxDistance = audioSource.InitialMaxDistance;
        }
        foreach (var parentConstraint in _scaledParentConstraints)
        {
            parentConstraint.Component.translationAtRest = parentConstraint.InitialTranslationAtRest;

            for (int i = 0; i < parentConstraint.InitialTranslationOffsets.Count; i++)
            {
                parentConstraint.Component.translationOffsets[i] = parentConstraint.InitialTranslationOffsets[i];
            }
        }
        foreach (var positionConstraint in _scaledPositionConstraints)
        {
            positionConstraint.Component.translationAtRest = positionConstraint.InitialTranslationAtRest;
            positionConstraint.Component.translationOffset = positionConstraint.InitialTranslationOffset;
        }
        foreach (var scaleConstraint in _scaledScaleConstraints)
        {
            scaleConstraint.Component.scaleAtRest = scaleConstraint.InitialScaleAtRest;
            scaleConstraint.Component.scaleOffset = scaleConstraint.InitialScaleOffset;
        }
    }
}