using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class SkinnedTransformHider : ITransformHider
{
    private static readonly int s_Pos = Shader.PropertyToID("pos");
    private static readonly int s_BufferLayout = Shader.PropertyToID("bufferLayout");
    private static readonly int s_WeightedCount = Shader.PropertyToID("weightedCount");
    private static readonly int s_WeightedVertices = Shader.PropertyToID("weightedVertices");
    private static readonly int s_VertexBuffer = Shader.PropertyToID("VertexBuffer");

    // lame 2 frame init stuff
    private const int FrameInitCount = 0;
    private int _frameInitCounter;
    private bool _hasInitialized;
    private bool _markedForDeath;
    
    // mesh & bone
    private readonly SkinnedMeshRenderer _mainMesh;
    private readonly Transform _rootBone;
    
    // main hider stuff
    private GraphicsBuffer _graphicsBuffer;
    private int _bufferLayout;
    
    // subtasks
    private readonly List<SubTask> _subTasks = new();
    
    #region ITransformHider Methods
    
    public bool IsActive { get; set; } = true; // default hide, but FPRExclusion can override
    
    // anything player can touch is suspect to death
    public bool IsValid => !_markedForDeath && _mainMesh != null && _rootBone != null;
    
    public SkinnedTransformHider(SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions)
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        _mainMesh = renderer;
        
        if (_mainMesh == null
            || _mainMesh.sharedMesh == null
            || _mainMesh.sharedMaterials == null
            || _mainMesh.sharedMaterials.Length == 0)
        {
            Dispose();
            return; // no mesh or bone!
        }
        
        _rootBone = _mainMesh.rootBone;
        _rootBone ??= _mainMesh.transform; // fallback to transform if no root bone
        
        // log current time
        ShadowCloneMod.Logger.Msg($"SkinnedTransformHider part 1 in {sw.ElapsedMilliseconds}ms");
        
        SubTask.FindExclusionVertList(renderer, exclusions);

        foreach (var exclusion in exclusions)
        {
            FPRExclusion fprExclusion = exclusion.Value;
            if (fprExclusion.affectedVertexIndices.Count == 0)
                continue; // no affected verts
            
            SubTask subTask = new(this, fprExclusion, fprExclusion.affectedVertexIndices);
            _subTasks.Add(subTask);
            fprExclusion.relatedTasks.Add(subTask);
            fprExclusion.affectedVertexIndices.Clear(); // clear list for next SkinnedTransformHider
        }
        
        // log current time
        ShadowCloneMod.Logger.Msg($"SkinnedTransformHider part 3 in {sw.ElapsedMilliseconds}ms");

        if (_subTasks.Count == 0)
        {
            Dispose(); // had the bones, but not the weights :?
            ShadowCloneMod.Logger.Warning("SkinnedTransformHider No valid exclusions found!");
        }
        
        sw.Stop();
        
        ShadowCloneMod.Logger.Msg($"SkinnedTransformHider created in {sw.ElapsedMilliseconds}ms");
    }
    
    public bool Process()
    {
        bool shouldRender = _mainMesh.enabled && _mainMesh.gameObject.activeInHierarchy;
        
        // GraphicsBuffer becomes stale when mesh is disabled
        if (!shouldRender)
        {
            _frameInitCounter = 0;
            _hasInitialized = false;
            return false;
        }
        
        // Unity is weird, so we need to wait 2 frames before we can get the graphics buffer
        if (_frameInitCounter >= FrameInitCount)
        {
            if (_hasInitialized) 
                return true;
            
            _hasInitialized = true;
            SetupGraphicsBuffer();
            return true;
        }
        
        _mainMesh.forceRenderingOff = true; // force off if mesh is disabled
        
        _frameInitCounter++;
        return false;
    }

    public bool PostProcess()
        => false; // not needed

    public void HideTransform(bool forced = false)
    {
        _mainMesh.forceRenderingOff = false;
        
        _graphicsBuffer = _mainMesh.GetVertexBuffer();
        
        foreach (SubTask subTask in _subTasks)
            if ((forced || subTask.IsActive) && subTask.IsValid) 
                subTask.Dispatch();

        _graphicsBuffer.Release();
    }

    public void ShowTransform()
    {
        // not needed
    }

    public void Dispose()
    {
        _markedForDeath = true;
        foreach (SubTask subTask in _subTasks)
            subTask.Dispose();
        
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
    }
    
    #endregion

    #region Private Methods

    // Unity is weird, so we need to wait 2 frames before we can get the graphics buffer
    private void SetupGraphicsBuffer()
    {
        Mesh mesh = _mainMesh.sharedMesh;

        _bufferLayout = 0;
        if (mesh.HasVertexAttribute(VertexAttribute.Position)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Normal)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Tangent)) _bufferLayout += 4;
        // ComputeShader is doing bitshift so we dont need to multiply by 4
        
        _mainMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }

    #endregion

    #region Sub Task Class

    private class SubTask : IFPRExclusionTask
    {
        public bool IsActive { get; set; } = true;
        public bool IsValid => _computeBuffer != null; // TODO: cleanup dead tasks
        
        private readonly SkinnedTransformHider _parent;
        private readonly Transform _shrinkBone;
        private readonly int _vertexCount;
        private readonly ComputeBuffer _computeBuffer;
        private readonly int _threadGroups;

        public SubTask(SkinnedTransformHider parent, FPRExclusion exclusion, List<int> exclusionVerts)
        {
            _parent = parent;
            _shrinkBone = exclusion.target;
        
            _vertexCount = exclusionVerts.Count;
            _computeBuffer = new ComputeBuffer(_vertexCount, sizeof(int));
            _computeBuffer.SetData(exclusionVerts.ToArray());
            
            const float xThreadGroups = 64f;
            _threadGroups = Mathf.CeilToInt(_vertexCount / xThreadGroups);
        }

        public void Dispatch()
        {
            Vector3 pos = _parent._rootBone.transform.InverseTransformPoint(_shrinkBone.position) * _parent._rootBone.lossyScale.y;
            TransformHiderManager.shader.SetVector(s_Pos, pos);
            TransformHiderManager.shader.SetInt(s_WeightedCount, _vertexCount);
            TransformHiderManager.shader.SetInt(s_BufferLayout, _parent._bufferLayout);
            TransformHiderManager.shader.SetBuffer(0, s_WeightedVertices, _computeBuffer);
            TransformHiderManager.shader.SetBuffer(0, s_VertexBuffer, _parent._graphicsBuffer);
            TransformHiderManager.shader.Dispatch(0, _threadGroups, 1, 1);
        }
        
        public void Dispose()
        {
            _computeBuffer?.Dispose();
        }

        #region Private Methods
        
        public static void FindExclusionVertList(SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions) 
        {
            var boneWeights = renderer.sharedMesh.boneWeights;
        
            HashSet<int> weights = new();
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                // if bone == any key in exclusions, add to weights
                if (!exclusions.TryGetValue(renderer.bones[i], out FPRExclusion _)) 
                    continue;
                
                weights.Add(i);
            }

            for (int i = 0; i < boneWeights.Length; i++) 
            {
                BoneWeight weight = boneWeights[i];
                
                Transform bone = null;
                const float minWeightThreshold = 0.2f;
                if (weights.Contains(weight.boneIndex0) && weight.weight0 > minWeightThreshold)
                    bone = renderer.bones[weight.boneIndex0];
                else if (weights.Contains(weight.boneIndex1) && weight.weight1 > minWeightThreshold)
                    bone = renderer.bones[weight.boneIndex1];
                else if (weights.Contains(weight.boneIndex2) && weight.weight2 > minWeightThreshold)
                    bone = renderer.bones[weight.boneIndex2];
                else if (weights.Contains(weight.boneIndex3) && weight.weight3 > minWeightThreshold)
                    bone = renderer.bones[weight.boneIndex3];

                if (bone == null) continue; // no bone found
                
                // add vertex to exclusion list
                exclusions[bone].affectedVertexIndices.Add(i);
            }
        }
        
        #endregion
    }
    
    #endregion
}