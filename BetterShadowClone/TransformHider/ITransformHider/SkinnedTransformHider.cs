using System;
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
        
        // subtask creation
        
        var bones = renderer.bones;
        List<FPRExclusion> fprExclusions = new();
            
        foreach (Transform bone in bones)
        {
            if (bone == null) 
                continue; // thanks AdvancedSafety for preventing null ref for so long...
            
            if (!exclusions.TryGetValue(bone, out FPRExclusion exclusion)) 
                continue;
            
            fprExclusions.Add(exclusion);
        }

        List<int> exclusionVerts;
        foreach (FPRExclusion exclusion in fprExclusions)
        {
            exclusionVerts = SubTask.FindExclusionVertList(renderer, exclusion);
            if (exclusionVerts.Count == 0) 
                continue;
            
            SubTask subTask = new(this, exclusion, exclusionVerts);
            _subTasks.Add(subTask);
            exclusion.relatedTasks.Add(subTask);
        }
        
        if (_subTasks.Count == 0) 
            Dispose(); // had the bones, but not the weights :?
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
        
        private readonly FPRExclusion _exclusion;
        
        public SubTask(SkinnedTransformHider parent, FPRExclusion exclusion, List<int> exclusionVerts)
        {
            _parent = parent;
            _exclusion = exclusion;
            _shrinkBone = _exclusion.target;
        
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
        
        public static List<int> FindExclusionVertList(SkinnedMeshRenderer renderer, FPRExclusion exclusion) 
        {
            var boneWeights = renderer.sharedMesh.boneWeights;
            var bones = exclusion.affectedChildren;
        
            HashSet<int> weights = new();
            for (int i = 0; i < renderer.bones.Length; i++)
                if (bones.Contains(renderer.bones[i])) weights.Add(i);

            List<int> headVertices = new();
            for (int i = 0; i < boneWeights.Length; i++) 
            {
                BoneWeight weight = boneWeights[i];
                const float minWeightThreshold = 0.2f;
                if (weights.Contains(weight.boneIndex0) && weight.weight0 > minWeightThreshold
                    || weights.Contains(weight.boneIndex1) && weight.weight1 > minWeightThreshold
                    || weights.Contains(weight.boneIndex2) && weight.weight2 > minWeightThreshold
                    || weights.Contains(weight.boneIndex3) && weight.weight3 > minWeightThreshold)
                    headVertices.Add(i);
            }
        
            return headVertices;
        }
        
        #endregion
    }
    
    #endregion
}