using System.Reflection;
using UnityEngine;

namespace NAK.OriginShift.Utility;

/// <summary>
/// Dumb little utility class to access the private staticBatchRootTransform property in the Renderer class.
/// Using this we can move static batched objects with the scene! :)
/// </summary>
public static class RendererReflectionUtility
{
    private static readonly PropertyInfo _staticBatchRootTransformProperty;

    static RendererReflectionUtility()
    {
        Type rendererType = typeof(Renderer);
        _staticBatchRootTransformProperty = rendererType.GetProperty("staticBatchRootTransform", BindingFlags.NonPublic | BindingFlags.Instance);
        if (_staticBatchRootTransformProperty == null) OriginShiftMod.Logger.Error("Property staticBatchRootTransform not found in Renderer class.");
    }

    public static void SetStaticBatchRootTransform(Renderer renderer, Transform newTransform)
    {
        if (_staticBatchRootTransformProperty != null)
            _staticBatchRootTransformProperty.SetValue(renderer, newTransform);
    }

    public static Transform GetStaticBatchRootTransform(Renderer renderer)
    {
        if (_staticBatchRootTransformProperty != null)
            return (Transform)_staticBatchRootTransformProperty.GetValue(renderer);
        return null;
    }
}
