using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod.ScaledComponents;

public class ScaledAudioSource
{
    public AudioSource Component { get; }
    public float InitialMinDistance { get; }
    public float InitialMaxDistance { get; }

    public ScaledAudioSource(AudioSource component)
    {
        Component = component;
        InitialMinDistance = component.minDistance;
        InitialMaxDistance = component.maxDistance;
    }
}

public class ScaledLight
{
    public Light Component { get; }
    public float InitialRange { get; }

    public ScaledLight(Light component)
    {
        Component = component;
        InitialRange = component.range;
    }
}

public class ScaledPositionConstraint
{
    public PositionConstraint Component { get; }
    public Vector3 InitialTranslationAtRest { get; }
    public Vector3 InitialTranslationOffset { get; }

    public ScaledPositionConstraint(PositionConstraint component)
    {
        Component = component;
        InitialTranslationAtRest = component.translationAtRest;
        InitialTranslationOffset = component.translationOffset;
    }
}

public class ScaledParentConstraint
{
    public ParentConstraint Component { get; }
    public Vector3 InitialTranslationAtRest { get; }
    public List<Vector3> InitialTranslationOffsets { get; }

    public ScaledParentConstraint(ParentConstraint component)
    {
        Component = component;
        InitialTranslationAtRest = component.translationAtRest;
        InitialTranslationOffsets = component.translationOffsets.ToList();
    }
}

public class ScaledScaleConstraint
{
    public ScaleConstraint Component { get; }
    public Vector3 InitialScaleAtRest { get; }
    public Vector3 InitialScaleOffset { get; }

    public ScaledScaleConstraint(ScaleConstraint component)
    {
        Component = component;
        InitialScaleAtRest = component.scaleAtRest;
        InitialScaleOffset = component.scaleOffset;
    }
}