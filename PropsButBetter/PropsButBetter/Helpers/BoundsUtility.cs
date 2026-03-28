using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Buffers;
using Unity.Collections.LowLevel.Unsafe;

public static class MeshBoundsUtility
{
    [BurstCompile]
    unsafe struct BoundsJob : IJob
    {
        [ReadOnly] public NativeArray<byte> bytes;
        public int vertexCount;
        public int stride;
        public int positionOffset;
        public NativeArray<float3> minMax;

        public void Execute()
        {
            byte* basePtr = (byte*)bytes.GetUnsafeReadOnlyPtr();

            float3 min = *(float3*)(basePtr + positionOffset);
            float3 max = min;

            for (int i = 1; i < vertexCount; i++)
            {
                float3 p = *(float3*)(basePtr + i * stride + positionOffset);
                min = math.min(min, p);
                max = math.max(max, p);
            }

            minMax[0] = min;
            minMax[1] = max;
        }
    }

    public static Bounds CalculateTightBounds(Mesh mesh)
    {
        if (!mesh || mesh.vertexCount == 0)
            return default;

        var attrs = mesh.GetVertexAttributes();

        int stream = -1;
        int positionOffset = 0;

        for (int i = 0; i < attrs.Length; i++)
        {
            var a = attrs[i];

            if (a.attribute == VertexAttribute.Position)
            {
                stream = a.stream;
                break;
            }

            if (stream == -1 || a.stream == stream)
            {
                int size = a.format switch
                {
                    VertexAttributeFormat.Float32 => 4,
                    VertexAttributeFormat.Float16 => 2,
                    VertexAttributeFormat.UNorm8  => 1,
                    VertexAttributeFormat.SNorm8  => 1,
                    VertexAttributeFormat.UNorm16 => 2,
                    VertexAttributeFormat.SNorm16 => 2,
                    VertexAttributeFormat.UInt8   => 1,
                    VertexAttributeFormat.SInt8   => 1,
                    VertexAttributeFormat.UInt16  => 2,
                    VertexAttributeFormat.SInt16  => 2,
                    VertexAttributeFormat.UInt32  => 4,
                    VertexAttributeFormat.SInt32  => 4,
                    _ => 0
                };

                positionOffset += size * a.dimension;
            }
        }

        if (stream < 0)
            return default;

        using GraphicsBuffer vb = mesh.GetVertexBuffer(stream);
        int stride = vb.stride;
        int byteCount = vb.count * stride;

        // REQUIRED: managed array
        byte[] managedBytes = ArrayPool<byte>.Shared.Rent(byteCount);
        vb.GetData(managedBytes, 0, 0, byteCount);

        var bytes = new NativeArray<byte>(
            byteCount,
            Allocator.TempJob,
            NativeArrayOptions.UninitializedMemory);

        NativeArray<byte>.Copy(managedBytes, bytes, byteCount);
        ArrayPool<byte>.Shared.Return(managedBytes);

        var minMax = new NativeArray<float3>(2, Allocator.TempJob);

        new BoundsJob
        {
            bytes = bytes,
            vertexCount = mesh.vertexCount,
            stride = stride,
            positionOffset = positionOffset,
            minMax = minMax
        }.Run();

        bytes.Dispose();

        float3 min = minMax[0];
        float3 max = minMax[1];
        minMax.Dispose();

        return new Bounds((min + max) * 0.5f, max - min);
    }
    
    public static bool TryCalculateRendererBounds(Renderer renderer, out Bounds worldBounds)
    {
        worldBounds = default;
        switch (renderer)
        {
            case MeshRenderer mr:
            {
                if (!renderer.TryGetComponent(out MeshFilter mf))
                    return false;
                
                Mesh sharedMesh = mf.sharedMesh;
                if (!sharedMesh)
                    return false;
                
                Bounds local = CalculateTightBounds(sharedMesh);
                if (local.size == Vector3.zero)
                    return false;
                
                worldBounds = TransformBounds(mr.transform, local);
                return true;
            }

            case SkinnedMeshRenderer smr:
            {
                Mesh sharedMesh = smr.sharedMesh;
                if (!sharedMesh)
                    return false;
                
                Bounds local = CalculateTightBounds(sharedMesh);
                if (local.size == Vector3.zero)
                    return false;

                worldBounds = TransformBounds(smr.transform, local);
                return true;
            }

            default:
            {
                worldBounds = renderer.bounds;
                return true;
            }
        }
    }
    
    /// <summary>
    /// Calculates the combined world-space bounds of all Renderers under a GameObject.
    /// </summary>
    /// <param name="root">The root GameObject to search under.</param>
    /// <param name="includeInactive">Whether to include inactive GameObjects.</param>
    /// <returns>Combined bounds, or default if no renderers found.</returns>
    public static Bounds CalculateCombinedBounds(GameObject root, bool includeInactive = false)
    {
        if (!root)
            return default;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);

        if (renderers.Length == 0)
            return default;

        // Find first valid bounds
        int startIndex = 0;
        Bounds combined = default;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] && renderers[i].enabled)
            {
                combined = renderers[i].bounds;
                startIndex = i + 1;
                break;
            }
        }

        // Encapsulate remaining renderers
        for (int i = startIndex; i < renderers.Length; i++)
        {
            if (renderers[i] && renderers[i].enabled)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
        }

        return combined;
    }

    /// <summary>
    /// Calculates tight combined bounds using mesh vertex data instead of renderer bounds.
    /// More accurate but slower than CalculateCombinedBounds.
    /// </summary>
    /// <param name="root">The root GameObject to search under.</param>
    /// <param name="includeInactive">Whether to include inactive GameObjects.</param>
    /// <returns>Combined tight bounds in world space, or default if no valid meshes found.</returns>
    public static Bounds CalculateCombinedTightBounds(GameObject root, bool includeInactive = false)
    {
        if (!root)
            return default;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactive);
        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);

        bool hasAny = false;
        Bounds combined = default;

        // Process MeshFilters
        foreach (var mf in meshFilters)
        {
            if (!mf || !mf.sharedMesh)
                continue;

            Bounds localBounds = CalculateTightBounds(mf.sharedMesh);
            if (localBounds.size == Vector3.zero)
                continue;

            Bounds worldBounds = TransformBounds(mf.transform, localBounds);

            if (!hasAny)
            {
                combined = worldBounds;
                hasAny = true;
            }
            else
            {
                combined.Encapsulate(worldBounds);
            }
        }

        // Process SkinnedMeshRenderers
        foreach (var smr in skinnedRenderers)
        {
            if (!smr || !smr.sharedMesh)
                continue;

            // For skinned meshes, use the current baked bounds or renderer bounds
            // since vertex positions change with animation
            Bounds worldBounds = smr.bounds;

            if (!hasAny)
            {
                combined = worldBounds;
                hasAny = true;
            }
            else
            {
                combined.Encapsulate(worldBounds);
            }
        }

        return combined;
    }

    /// <summary>
    /// Transforms an AABB from local space to world space, returning a new AABB that fully contains the transformed box.
    /// </summary>
    private static Bounds TransformBounds(Transform transform, Bounds localBounds)
    {
        Vector3 center = transform.TransformPoint(localBounds.center);
        Vector3 extents = localBounds.extents;

        // Transform each axis extent by the absolute value of the rotation/scale matrix
        Vector3 axisX = transform.TransformVector(extents.x, 0, 0);
        Vector3 axisY = transform.TransformVector(0, extents.y, 0);
        Vector3 axisZ = transform.TransformVector(0, 0, extents.z);

        Vector3 worldExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
        );

        return new Bounds(center, worldExtents * 2f);
    }
}
