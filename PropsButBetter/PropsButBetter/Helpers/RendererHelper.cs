using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class RendererHelper
{
    private static readonly Func<Renderer, int> _getMaterialCount;

    static RendererHelper()
    {
        // Find the private method
        MethodInfo mi = typeof(Renderer).GetMethod(
            "GetMaterialCount",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (mi != null)
        {
            // Create a fast delegate
            _getMaterialCount = (Func<Renderer, int>)Delegate.CreateDelegate(
                typeof(Func<Renderer, int>),
                null,
                mi
            );
        }
    }

    public static int GetMaterialCount(this Renderer renderer)
    {
        return _getMaterialCount?.Invoke(renderer) ?? 0;
    }
}