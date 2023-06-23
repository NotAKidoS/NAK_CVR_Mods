using ABI.CCK.Components;
using NAK.AvatarScaleMod.ScaledComponents;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod;

public class AvatarScaleManager : MonoBehaviour
{
    public static AvatarScaleManager LocalAvatar { get; private set; }

    // List of component types to be collected and scaled
    private static readonly System.Type[] scaleComponentTypes = new System.Type[]
    {
        typeof(Light),
        typeof(AudioSource),
        typeof(ParticleSystem),
        typeof(ParentConstraint),
        typeof(PositionConstraint),
        typeof(ScaleConstraint),
    };

    public const float MinimumHeight = 0.1f;
    public const float MaximumHeight = 10f;

    // Scalable Components
    private List<ScaledLight> _lights = new List<ScaledLight>();
    private List<ScaledAudioSource> _audioSources = new List<ScaledAudioSource>();
    //private List<ScaledComponent<ParticleSystem>> _particleSystems = new List<ScaledComponent<ParticleSystem>>();
    private List<ScaledParentConstraint> _parentConstraints = new List<ScaledParentConstraint>();
    private List<ScaledPositionConstraint> _positionConstraints = new List<ScaledPositionConstraint>();
    private List<ScaledScaleConstraint> _scaleConstraints = new List<ScaledScaleConstraint>();

    public float TargetHeight { get; private set; }
    public float InitialHeight { get; private set; }
    public Vector3 InitialScale { get; private set; }
    public float ScaleFactor { get; private set; }

    public void Initialize(float initialHeight, Vector3 initialScale)
    {
        // Check for zero height
        if (Math.Abs(initialHeight) < 1E-6)
        {
            AvatarScaleMod.Logger.Warning("Cannot initialize with a height of zero!");
            return;
        }

        this.TargetHeight = 1f;
        this.InitialHeight = initialHeight;
        this.InitialScale = initialScale;
        UpdateScaleFactor();
    }

    public void SetTargetHeight(float newHeight)
    {
        TargetHeight = Mathf.Clamp(newHeight, MinimumHeight, MaximumHeight);
        UpdateScaleFactor();
    }

    public void UpdateScaleFactor()
    {
        // Check for zero
        if (Math.Abs(InitialHeight) < 1E-6)
        {
            AvatarScaleMod.Logger.Warning("InitialHeight is zero, cannot calculate ScaleFactor.");
            return;
        }

        this.ScaleFactor = TargetHeight / InitialHeight;
    }

    private Vector3 CalculateNewScale()
    {
        return InitialScale * ScaleFactor;
    }

    private void Awake()
    {
        // why am i caching the avatar
        CVRAvatar avatar = GetComponent<CVRAvatar>();
        if (avatar == null)
        {
            AvatarScaleMod.Logger.Error("AvatarScaleManager should be attached to a GameObject with a CVRAvatar component.");
            return;
        }

        // i cant believe i would stoop this low
        if (gameObject.layer == 8 && LocalAvatar == null)
            LocalAvatar = this;

        FindComponentsOfType(scaleComponentTypes);
    }

    private void OnDestroy()
    {
        _audioSources.Clear();
        _lights.Clear();
        //_particleSystems.Clear(); // fuck no
        _parentConstraints.Clear();
        _positionConstraints.Clear();
        _scaleConstraints.Clear();

        // local player manager
        if (LocalAvatar == this)
            LocalAvatar = null;
    }

    private void OnDisable()
    {
        ResetAllToInitialScale();
    }

    private void FindComponentsOfType(params System.Type[] types)
    {
        foreach (var type in types)
        {
            var components = gameObject.GetComponentsInChildren(type, true);
            foreach (var component in components)
            {
                switch (component)
                {
                    case AudioSource audioSource:
                        _audioSources.Add(new ScaledAudioSource(audioSource));
                        break;
                    case Light light:
                        _lights.Add(new ScaledLight(light));
                        break;
                    case ParentConstraint parentConstraint:
                        _parentConstraints.Add(new ScaledParentConstraint(parentConstraint));
                        break;
                    case PositionConstraint positionConstraint:
                        _positionConstraints.Add(new ScaledPositionConstraint(positionConstraint));
                        break;
                    case ScaleConstraint scaleConstraint:
                        _scaleConstraints.Add(new ScaledScaleConstraint(scaleConstraint));
                        break;
                }
            }
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
        transform.localScale = CalculateNewScale();
    }

    private void ApplyComponentScaling()
    {
        UpdateLightScales();
        UpdateAudioSourceScales();
        UpdateParentConstraintScales();
        UpdatePositionConstraintScales();
        UpdateScaleConstraintScales();
    }

    private void UpdateLightScales()
    {
        foreach (var scaledLight in _lights)
        {
            scaledLight.Component.range = scaledLight.InitialRange * ScaleFactor;
        }
    }

    private void UpdateAudioSourceScales()
    {
        foreach (var scaledAudioSource in _audioSources)
        {
            scaledAudioSource.Component.minDistance = scaledAudioSource.InitialMinDistance * ScaleFactor;
            scaledAudioSource.Component.maxDistance = scaledAudioSource.InitialMaxDistance * ScaleFactor;
        }
    }

    private void UpdateParentConstraintScales()
    {
        foreach (var scaledParentConstraint in _parentConstraints)
        {
            scaledParentConstraint.Component.translationAtRest = scaledParentConstraint.InitialTranslationAtRest * ScaleFactor;

            for (int i = 0; i < scaledParentConstraint.InitialTranslationOffsets.Count; i++)
            {
                scaledParentConstraint.Component.translationOffsets[i] = scaledParentConstraint.InitialTranslationOffsets[i] * ScaleFactor;
            }
        }
    }

    private void UpdatePositionConstraintScales()
    {
        foreach (var scaledPositionConstraint in _positionConstraints)
        {
            scaledPositionConstraint.Component.translationAtRest = scaledPositionConstraint.InitialTranslationAtRest * ScaleFactor;
            scaledPositionConstraint.Component.translationOffset = scaledPositionConstraint.InitialTranslationOffset * ScaleFactor;
        }
    }

    private void UpdateScaleConstraintScales()
    {
        foreach (var scaledScaleConstraint in _scaleConstraints)
        {
            scaledScaleConstraint.Component.scaleAtRest = scaledScaleConstraint.InitialScaleAtRest * ScaleFactor;
            scaledScaleConstraint.Component.scaleOffset = scaledScaleConstraint.InitialScaleOffset * ScaleFactor;
        }
    }

    private void ResetAllToInitialScale()
    {
        // quick n lazy for right now
        transform.localScale = InitialScale;

        foreach (var scaledLight in _lights)
        {
            scaledLight.Component.range = scaledLight.InitialRange;
        }
        foreach (var scaledAudioSource in _audioSources)
        {
            scaledAudioSource.Component.minDistance = scaledAudioSource.InitialMinDistance;
            scaledAudioSource.Component.maxDistance = scaledAudioSource.InitialMaxDistance;
        }
        foreach (var scaledParentConstraint in _parentConstraints)
        {
            scaledParentConstraint.Component.translationAtRest = scaledParentConstraint.InitialTranslationAtRest;

            for (int i = 0; i < scaledParentConstraint.InitialTranslationOffsets.Count; i++)
            {
                scaledParentConstraint.Component.translationOffsets[i] = scaledParentConstraint.InitialTranslationOffsets[i];
            }
        }
        foreach (var scaledPositionConstraint in _positionConstraints)
        {
            scaledPositionConstraint.Component.translationAtRest = scaledPositionConstraint.InitialTranslationAtRest;
            scaledPositionConstraint.Component.translationOffset = scaledPositionConstraint.InitialTranslationOffset;
        }
        foreach (var scaledScaleConstraint in _scaleConstraints)
        {
            scaledScaleConstraint.Component.scaleAtRest = scaledScaleConstraint.InitialScaleAtRest;
            scaledScaleConstraint.Component.scaleOffset = scaledScaleConstraint.InitialScaleOffset;
        }
    }

    // use for slow transition between avatars initial height & saved height>>>??????????????
    public IEnumerator SetTargetHeightOverTime(float newHeight, float duration)
    {
        float startTime = Time.time;
        float startHeight = TargetHeight;
        newHeight = Mathf.Clamp(newHeight, MinimumHeight, MaximumHeight);

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            TargetHeight = Mathf.Lerp(startHeight, newHeight, t);
            UpdateScaleFactor();
            yield return null;
        }

        TargetHeight = newHeight;
        UpdateScaleFactor();
    }
}