using UnityEngine;
using UnityEngine.Animations;

namespace NAK.AvatarScaleMod.ScaledComponents;

public class ScaledAudioSource
{
    private readonly AudioSource Component;
    private readonly float InitialMinDistance;
    private readonly float InitialMaxDistance;

    public ScaledAudioSource(AudioSource component)
    {
        Component = component;
        InitialMinDistance = component.minDistance;
        InitialMaxDistance = component.maxDistance;
    }

    public void Scale(float scaleFactor)
    {
        Component.minDistance = InitialMinDistance * scaleFactor;
        Component.maxDistance = InitialMaxDistance * scaleFactor;
    }

    public void Reset()
    {
        Component.minDistance = InitialMinDistance;
        Component.maxDistance = InitialMaxDistance;
    }
}

public class ScaledLight
{
    private readonly Light Component;
    private readonly float InitialRange;

    public ScaledLight(Light component)
    {
        Component = component;
        InitialRange = component.range;
    }

    public void Scale(float scaleFactor)
    {
        Component.range = InitialRange * scaleFactor;
    }

    public void Reset()
    {
        Component.range = InitialRange;

    }
}

public class ScaledPositionConstraint
{
    private readonly PositionConstraint Component;
    private readonly Vector3 InitialTranslationAtRest;
    private readonly Vector3 InitialTranslationOffset;

    public ScaledPositionConstraint(PositionConstraint component)
    {
        Component = component;
        InitialTranslationAtRest = component.translationAtRest;
        InitialTranslationOffset = component.translationOffset;
    }

    public void Scale(float scaleFactor)
    {
        Component.translationAtRest = InitialTranslationAtRest * scaleFactor;
        Component.translationOffset = InitialTranslationOffset * scaleFactor;
    }

    public void Reset()
    {
        Component.translationAtRest = InitialTranslationAtRest;
        Component.translationOffset = InitialTranslationOffset;
    }
}

public class ScaledParentConstraint
{
    private readonly ParentConstraint Component;
    private readonly Vector3 InitialTranslationAtRest;
    private readonly List<Vector3> InitialTranslationOffsets;

    public ScaledParentConstraint(ParentConstraint component)
    {
        Component = component;
        InitialTranslationAtRest = component.translationAtRest;
        InitialTranslationOffsets = component.translationOffsets.ToList();
    }

    public void Scale(float scaleFactor)
    {
        Component.translationAtRest = InitialTranslationAtRest * scaleFactor;
        for (int i = 0; i < InitialTranslationOffsets.Count; i++)
            Component.translationOffsets[i] = InitialTranslationOffsets[i] * scaleFactor;
    }

    public void Reset()
    {
        Component.translationAtRest = InitialTranslationAtRest;
        for (int i = 0; i < InitialTranslationOffsets.Count; i++)
            Component.translationOffsets[i] = InitialTranslationOffsets[i];
    }
}

public class ScaledScaleConstraint
{
    private readonly ScaleConstraint Component;
    private readonly Vector3 InitialScaleAtRest;
    private readonly Vector3 InitialScaleOffset;

    public ScaledScaleConstraint(ScaleConstraint component)
    {
        Component = component;
        InitialScaleAtRest = component.scaleAtRest;
        InitialScaleOffset = component.scaleOffset;
    }

    public void Scale(float scaleFactor)
    {
        Component.scaleAtRest = InitialScaleAtRest * scaleFactor;
        // Component.scaleOffset = InitialScaleOffset * scaleFactor;
    }

    public void Reset()
    {
        Component.scaleAtRest = InitialScaleAtRest;
        Component.scaleOffset = InitialScaleOffset;
    }
}