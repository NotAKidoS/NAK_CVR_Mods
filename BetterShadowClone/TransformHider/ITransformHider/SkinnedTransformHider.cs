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
    private readonly Transform _shrinkBone;
    private readonly SkinnedMeshRenderer _mainMesh;
    private readonly Transform _rootBone;
    
    // exclusion
    private readonly FPRExclusion _exclusion;
    
    // hider stuff
    private GraphicsBuffer _graphicsBuffer;
    private int _bufferLayout;
    
    private ComputeBuffer _computeBuffer;
    private int _vertexCount;
    private int _threadGroups;

    #region ITransformHider Methods
    
    public bool IsActive { get; set; } = true; // default hide, but FPRExclusion can override
    
    // anything player can touch is suspect to death
    public bool IsValid => _mainMesh != null && _shrinkBone != null && !_markedForDeath;
    
    public SkinnedTransformHider(SkinnedMeshRenderer renderer, FPRExclusion exclusion)
    {
        _mainMesh = renderer;
        _shrinkBone = exclusion.target;
        _exclusion = exclusion;
        
        if (_exclusion == null 
            || _shrinkBone == null
            || _mainMesh == null
            || _mainMesh.sharedMesh == null
            || _mainMesh.sharedMaterials == null
            || _mainMesh.sharedMaterials.Length == 0)
        {
            Dispose();
            return; // no mesh or bone!
        }
        
        // find the head vertices
        var exclusionVerts = FindExclusionVertList();
        if (exclusionVerts.Count == 0)
        {
            Dispose();
            return; // no head vertices!
        }
        
        _rootBone = _mainMesh.rootBone;
        _rootBone ??= _mainMesh.transform; // fallback to transform if no root bone
        
        _vertexCount = exclusionVerts.Count;
        _computeBuffer = new ComputeBuffer(_vertexCount, sizeof(int));
        _computeBuffer.SetData(exclusionVerts.ToArray());
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
    {
        return false; // not needed
    }

    public void HideTransform()
    {
        _mainMesh.forceRenderingOff = false;
        
        // probably fine
        Vector3 pos = _rootBone.transform.InverseTransformPoint(_shrinkBone.position) * _rootBone.lossyScale.y;
        
        _graphicsBuffer = _mainMesh.GetVertexBuffer();
        TransformHiderManager.shader.SetVector(s_Pos, pos);
        TransformHiderManager.shader.SetInt(s_WeightedCount, _vertexCount);
        TransformHiderManager.shader.SetInt(s_BufferLayout, _bufferLayout);
        TransformHiderManager.shader.SetBuffer(0, s_WeightedVertices, _computeBuffer);
        TransformHiderManager.shader.SetBuffer(0, s_VertexBuffer, _graphicsBuffer);
        TransformHiderManager.shader.Dispatch(0, _threadGroups, 1, 1);
        _graphicsBuffer.Release();
    }

    public void ShowTransform()
    {
        // not needed
    }

    public void Dispose()
    {
        _markedForDeath = true;
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
        _computeBuffer?.Dispose();
        _computeBuffer = null;
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
        //_bufferLayout *= 4; // 4 bytes per float
        
        const float xThreadGroups = 64f;
        _threadGroups = Mathf.CeilToInt(mesh.vertexCount / xThreadGroups);
        
        _mainMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }
    
    private List<int> FindExclusionVertList() 
    {
        var boneWeights = _mainMesh.sharedMesh.boneWeights;
        var bones = _exclusion.affectedChildren;
        
        HashSet<int> weights = new(); //get indexs of child bones
        for (int i = 0; i < _mainMesh.bones.Length; i++)
            if (bones.Contains(_mainMesh.bones[i])) weights.Add(i);

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