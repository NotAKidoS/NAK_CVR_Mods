using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.RCCVirtualSteeringWheel.Util;

public class BoneVertexBoundsUtility : MonoBehaviour
{
    private static readonly ProfilerMarker s_calculateBoundsMarker = new("BoneVertexBounds.Calculate");
    private static readonly ProfilerMarker s_processChildRenderersMarker = new("BoneVertexBounds.ProcessChildRenderers");
    private static readonly ProfilerMarker s_processSkinnedMeshesMarker = new("BoneVertexBounds.ProcessSkinnedMeshes");
    private static readonly ProfilerMarker s_processVerticesMarker = new("BoneVertexBounds.ProcessVertices");
    private static readonly ProfilerMarker s_processConstraintsMarker = new("BoneVertexBounds.ProcessConstraints");
    private static readonly ProfilerMarker s_jobExecutionMarker = new("BoneVertexBounds.JobExecution");
    private static readonly ProfilerMarker s_meshCopyMarker = new("BoneVertexBounds.MeshCopy");
    
    private static BoneVertexBoundsUtility instance;
    
    private static BoneVertexBoundsUtility Instance
    {
        get
        {
            if (instance != null) return instance;
            GameObject go = new("BoneVertexBoundsUtility");
            instance = go.AddComponent<BoneVertexBoundsUtility>();
            DontDestroyOnLoad(go);
            return instance;
        }
    }
    
    [Flags]
    public enum BoundsCalculationFlags
    {
        None = 0,
        IncludeChildren = 1 << 0,
        IncludeSkinnedMesh = 1 << 1,
        IncludeConstraints = 1 << 2,
        All = IncludeChildren | IncludeSkinnedMesh | IncludeConstraints
    }

    public struct BoundsResult
    {
        public bool IsValid;
        public Bounds LocalBounds;
    }
    
    /// <summary>
    /// Calculates the bounds of a transform based on:
    /// - Children Renderers
    /// - Skinned Mesh Weights
    /// - Constrained Child Renderers & Skinned Mesh Weights (thanks Fearless)
    /// </summary>
    public static void CalculateBoneWeightedBounds(Transform bone, float weightThreshold, BoundsCalculationFlags flags, Action<BoundsResult> onComplete)
        => Instance.StartCoroutine(Instance.CalculateBoundsCoroutine(bone, weightThreshold, flags, onComplete));

    private IEnumerator CalculateBoundsCoroutine(Transform bone, float weightThreshold, BoundsCalculationFlags flags, Action<BoundsResult> onComplete)
    {
        using (s_calculateBoundsMarker.Auto())
        {
            BoundsResult result = new();
            var allWeightedPoints = new List<Vector3>();
            bool hasValidPoints = false;
            
            // Child renderers
            IEnumerator ProcessChildRenderersLocal(Transform targetBone, Action<List<Vector3>> onChildPoints)
            {
                using (s_processChildRenderersMarker.Auto())
                {
                    var points = new List<Vector3>();
                    var childRenderers = targetBone.GetComponentsInChildren<Renderer>()
                        .Where(r => r is not SkinnedMeshRenderer)
                        .ToArray();

                    foreach (Renderer childRend in childRenderers)
                    {
                        Bounds bounds = childRend.localBounds;
                        var corners = new Vector3[8];
                        Vector3 ext = bounds.extents;
                        Vector3 center = bounds.center;
                    
                        corners[0] = new Vector3(center.x - ext.x, center.y - ext.y, center.z - ext.z);
                        corners[1] = new Vector3(center.x + ext.x, center.y - ext.y, center.z - ext.z);
                        corners[2] = new Vector3(center.x - ext.x, center.y + ext.y, center.z - ext.z);
                        corners[3] = new Vector3(center.x + ext.x, center.y + ext.y, center.z - ext.z);
                        corners[4] = new Vector3(center.x - ext.x, center.y - ext.y, center.z + ext.z);
                        corners[5] = new Vector3(center.x + ext.x, center.y - ext.y, center.z + ext.z);
                        corners[6] = new Vector3(center.x - ext.x, center.y + ext.y, center.z + ext.z);
                        corners[7] = new Vector3(center.x + ext.x, center.y + ext.y, center.z + ext.z);

                        for (int i = 0; i < 8; i++)
                            points.Add(targetBone.InverseTransformPoint(childRend.transform.TransformPoint(corners[i])));
                    }

                    onChildPoints?.Invoke(points);
                }
                yield break;
            }

            // Skinned mesh renderers
            IEnumerator ProcessSkinnedMeshRenderersLocal(Transform targetBone, float threshold, Action<List<Vector3>> onSkinnedPoints)
            {
                using (s_processSkinnedMeshesMarker.Auto())
                {
                    var points = new List<Vector3>();
                    var siblingAndParentSkinnedMesh = targetBone.root.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var relevantMeshes = siblingAndParentSkinnedMesh.Where(smr => DoesMeshUseBone(smr, targetBone)).ToArray();

                    foreach (SkinnedMeshRenderer smr in relevantMeshes)
                    {
                        yield return StartCoroutine(ProcessSkinnedMesh(smr, targetBone, threshold, meshPoints =>
                        {
                            if (meshPoints is { Length: > 0 })
                                points.AddRange(meshPoints);
                        }));
                    }

                    onSkinnedPoints?.Invoke(points);
                }
            }

            // Constraints
            IEnumerator ProcessConstraintsLocal(Transform targetBone, float threshold, BoundsCalculationFlags constraintFlags, Action<List<Vector3>> onConstraintPoints)
            {
                using (s_processConstraintsMarker.Auto())
                {
                    var points = new List<Vector3>();
                    var processedTransforms = new HashSet<Transform>();
                    var constrainedTransforms = new List<Transform>();

                    // Find all constrained objects that reference our bone
                    var constraints = targetBone.root.GetComponentsInChildren<IConstraint>();

                    foreach (IConstraint constraint in constraints)
                    {
                        for (int i = 0; i < constraint.sourceCount; i++)
                        {
                            if (constraint.GetSource(i).sourceTransform != targetBone) continue;
                            constrainedTransforms.Add(((Behaviour)constraint).transform);
                            break;
                        }
                    }

                    // Process each constrained transform
                    foreach (Transform constrainedTransform in constrainedTransforms)
                    {
                        if (!processedTransforms.Add(constrainedTransform))
                            continue;

                        var localPoints = new List<Vector3>();
                        bool hasLocalPoints = false;

                        // Process child renderers if enabled
                        if ((constraintFlags & BoundsCalculationFlags.IncludeChildren) != 0)
                        {
                            yield return StartCoroutine(ProcessChildRenderersLocal(constrainedTransform, childPoints =>
                            {
                                if (childPoints is not { Count: > 0 }) return;
                                localPoints.AddRange(childPoints);
                                hasLocalPoints = true;
                            }));
                        }

                        // Process skinned mesh if enabled
                        if ((constraintFlags & BoundsCalculationFlags.IncludeSkinnedMesh) != 0)
                        {
                            yield return StartCoroutine(ProcessSkinnedMeshRenderersLocal(constrainedTransform, threshold, skinnedPoints =>
                            {
                                if (skinnedPoints is not { Count: > 0 }) return;
                                localPoints.AddRange(skinnedPoints);
                                hasLocalPoints = true;
                            }));
                        }

                        if (!hasLocalPoints) 
                            continue;
                        
                        // Convert all points to bone space
                        foreach (Vector3 point in localPoints)
                            points.Add(targetBone.InverseTransformPoint(constrainedTransform.TransformPoint(point)));
                    }

                    onConstraintPoints?.Invoke(points);
                }
            }

            // Process child renderers
            if ((flags & BoundsCalculationFlags.IncludeChildren) != 0)
            {
                yield return StartCoroutine(ProcessChildRenderersLocal(bone, childPoints =>
                {
                    if (childPoints is not { Count: > 0 }) return;
                    allWeightedPoints.AddRange(childPoints);
                    hasValidPoints = true;
                }));
            }

            // Process skinned mesh renderers
            if ((flags & BoundsCalculationFlags.IncludeSkinnedMesh) != 0)
            {
                yield return StartCoroutine(ProcessSkinnedMeshRenderersLocal(bone, weightThreshold, skinnedPoints =>
                {
                    if (skinnedPoints is not { Count: > 0 }) return;
                    allWeightedPoints.AddRange(skinnedPoints);
                    hasValidPoints = true;
                }));
            }

            // Process constraints
            if ((flags & BoundsCalculationFlags.IncludeConstraints) != 0)
            {
                // Use only Children and SkinnedMesh flags for constraint processing to prevent recursion (maybe make optional)?
                BoundsCalculationFlags constraintFlags = flags & ~BoundsCalculationFlags.IncludeConstraints;
                yield return StartCoroutine(ProcessConstraintsLocal(bone, weightThreshold, constraintFlags, constraintPoints =>
                {
                    if (constraintPoints is not { Count: > 0 }) return;
                    allWeightedPoints.AddRange(constraintPoints);
                    hasValidPoints = true;
                }));
            }

            if (!hasValidPoints)
            {
                result.IsValid = false;
                onComplete?.Invoke(result);
                yield break;
            }

            // Calculate final bounds in bone space
            Bounds bounds = new(allWeightedPoints[0], Vector3.zero);
            foreach (Vector3 point in allWeightedPoints)
                bounds.Encapsulate(point);

            // Ensure minimum size
            Vector3 size = bounds.size;
            size = Vector3.Max(size, Vector3.one * 0.01f);
            bounds.size = size;

            result.IsValid = true;
            result.LocalBounds = bounds;
            
            onComplete?.Invoke(result);
        }
    }

    private static bool DoesMeshUseBone(SkinnedMeshRenderer smr, Transform bone)
        => smr.bones != null && smr.bones.Contains(bone);

    private IEnumerator ProcessSkinnedMesh(SkinnedMeshRenderer smr, Transform bone, float weightThreshold, 
        Action<Vector3[]> onComplete)
    {
        Mesh mesh = smr.sharedMesh;
        if (mesh == null)
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Find bone index
        int boneIndex = Array.IndexOf(smr.bones, bone);
        if (boneIndex == -1)
        {
            onComplete?.Invoke(null);
            yield break;
        }
        
        Mesh meshToUse = mesh;
        GameObject tempGO = null;

        try 
        {
            // Handle non-readable meshes (ReadItAnyway lmao)
            if (!mesh.isReadable)
            {
                using (s_meshCopyMarker.Auto())
                {
                    tempGO = new GameObject("TempMeshReader");
                    SkinnedMeshRenderer tempSMR = tempGO.AddComponent<SkinnedMeshRenderer>();
                    tempSMR.sharedMesh = mesh;
                    meshToUse = new Mesh();
                    tempSMR.BakeMesh(meshToUse);
                }
            }

            Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshToUse);
            Mesh.MeshData meshData = meshDataArray[0];

            var vertexCount = meshData.vertexCount;
            var vertices = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
            var weights = new NativeArray<BoneWeight>(vertexCount, Allocator.TempJob);
            var results = new NativeArray<VertexResult>(vertexCount, Allocator.TempJob);

            meshData.GetVertices(vertices);
            weights.CopyFrom(mesh.boneWeights);
            
            // Debug.Log(vertices.Length);
            // Debug.Log(weights.Length);

            using (s_processVerticesMarker.Auto())
            {
                try
                {
                    Transform rootBone = smr.rootBone ? smr.rootBone.transform : smr.transform;
                    Matrix4x4 meshToWorld = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, rootBone.lossyScale);
                    
                    // Fixes setup where mesh was in diff hierarchy & 0.001 scale, bone & root bone outside & above
                    meshToWorld *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, smr.transform.localScale);
                    
                    ProcessVerticesJob processJob = new()
                    {
                        Vertices = vertices,
                        BoneWeights = weights,
                        Results = results,
                        BoneIndex = boneIndex,
                        WeightThreshold = weightThreshold,
                        MeshToWorld = meshToWorld,
                        WorldToBone = bone.worldToLocalMatrix
                    };

                    using (s_jobExecutionMarker.Auto())
                    {
                        int batchCount = Mathf.Max(1, vertexCount / 64);
                        JobHandle jobHandle = processJob.Schedule(vertexCount, batchCount);
                        while (!jobHandle.IsCompleted)
                            yield return null;
                        
                        jobHandle.Complete();
                    }

                    // Collect valid points
                    var validPoints = new List<Vector3>();
                    for (int i = 0; i < results.Length; i++)
                        if (results[i].IsValid) validPoints.Add(results[i].Position);

                    onComplete?.Invoke(validPoints.ToArray());
                }
                finally
                {
                    vertices.Dispose();
                    weights.Dispose();
                    results.Dispose();
                    meshDataArray.Dispose();
                }
            }
        }
        finally 
        {
            // Destroy duplicated baked mesh if we created one to read mesh data
            if (!mesh.isReadable && meshToUse != mesh) Destroy(meshToUse);
            if (tempGO != null) Destroy(tempGO);
        }
    }

    private struct VertexResult
    {
        public Vector3 Position;
        public bool IsValid;
    }

    [BurstCompile]
    private struct ProcessVerticesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> Vertices;
        [ReadOnly] public NativeArray<BoneWeight> BoneWeights;
        [NativeDisableParallelForRestriction]
        public NativeArray<VertexResult> Results;
        [ReadOnly] public int BoneIndex;
        [ReadOnly] public float WeightThreshold;
        [ReadOnly] public Matrix4x4 MeshToWorld;
        [ReadOnly] public Matrix4x4 WorldToBone;

        public void Execute(int i)
        {
            BoneWeight weight = BoneWeights[i];
            float totalWeight = 0f;

            if (weight.boneIndex0 == BoneIndex) totalWeight += weight.weight0;
            if (weight.boneIndex1 == BoneIndex) totalWeight += weight.weight1;
            if (weight.boneIndex2 == BoneIndex) totalWeight += weight.weight2;
            if (weight.boneIndex3 == BoneIndex) totalWeight += weight.weight3;

            if (totalWeight >= WeightThreshold)
            {
                // Transform vertex to bone space
                Vector3 worldPos = MeshToWorld.MultiplyPoint3x4(Vertices[i]);
                Vector3 boneLocalPos = WorldToBone.MultiplyPoint3x4(worldPos);
                
                Results[i] = new VertexResult
                {
                    Position = boneLocalPos,
                    IsValid = true
                };
            }
            else
            {
                Results[i] = new VertexResult { IsValid = false };
            }
        }
    }
}