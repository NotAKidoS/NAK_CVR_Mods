using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class SkinnedShadowClone : IShadowClone
{
    private static readonly int s_SourceBufferId = Shader.PropertyToID("_sourceBuffer");
    private static readonly int s_TargetBufferId = Shader.PropertyToID("_targetBuffer");
    private static readonly int s_HiddenVerticiesId = Shader.PropertyToID("_hiddenVertices");
    private static readonly int s_HiddenVertexPos = Shader.PropertyToID("_hiddenVertexPos");
    private static readonly int s_SourceBufferLayoutId = Shader.PropertyToID("_sourceBufferLayout");
    private static readonly int s_SourceRootMatrix = Shader.PropertyToID("_rootBoneMatrix");
    
    // lame 2 frame init stuff
    private const int FrameInitCount = 0;
    private int _frameInitCounter;
    private bool _hasInitialized;
    
    // shadow is used to cull ui, clone always exists
    private readonly bool _shouldCastShadows;
    private readonly SkinnedMeshRenderer _mainMesh;
    private readonly MeshRenderer _shadowMesh;
    private readonly MeshFilter _shadowMeshFilter;
    private readonly Transform _rootBone;
    
    // clone copying
    private GraphicsBuffer _graphicsBuffer;
    private GraphicsBuffer _targetBuffer;
    private ComputeBuffer _computeBuffer;
    private int _threadGroups;
    private int _bufferLayout;
    
    // material copying (unity is shit)
    private bool _hasShadowMaterials;
    private readonly Material[] _shadowMaterials;
    private readonly MaterialPropertyBlock _shadowMaterialBlock;

    #region IShadowClone Methods
    
    // anything player can touch is suspect to death
    public bool IsValid => _mainMesh != null && _shadowMesh != null && _rootBone != null;
    
    internal SkinnedShadowClone(SkinnedMeshRenderer renderer, FPRExclusion exclusion)
    {
        _mainMesh = renderer;
        
        if (_mainMesh == null
            || _mainMesh.sharedMesh == null
            || _mainMesh.sharedMaterials == null
            || _mainMesh.sharedMaterials.Length == 0)
        {
            Dispose();
            return; // no mesh!
        }
        
        
        FindExclusionVertList(_mainMesh, exclusion);
        
        if (exclusion.affectedVertexIndices.Count == 0)
        {
            Dispose();
            return; // no affected verts!
        }
        
        _computeBuffer = new ComputeBuffer(_mainMesh.sharedMesh.vertexCount, sizeof(int));
        _computeBuffer.SetData(exclusion.affectedVertexIndices.ToArray());
        exclusion.affectedVertexIndices.Clear();
        
        
        
        _shouldCastShadows = _mainMesh.shadowCastingMode != ShadowCastingMode.Off;
        //_mainMesh.shadowCastingMode = ShadowCastingMode.On; // visual mesh doesn't cast shadows
        
        (_shadowMesh, _shadowMeshFilter) = ShadowCloneManager.InstantiateShadowClone(_mainMesh);
        _shadowMesh.shadowCastingMode = ShadowCastingMode.Off; // shadow mesh doesn't cast shadows
        _shadowMesh.forceRenderingOff = false;

        
        _rootBone = _mainMesh.rootBone;
        _rootBone ??= _mainMesh.transform; // fallback to transform if no root bone
        
        // material copying shit
        int materialCount = _mainMesh.sharedMaterials.Length;
        Material shadowMaterial = ShadowCloneHelper.shadowMaterial;
        
        _shadowMaterialBlock = new MaterialPropertyBlock(); // TODO: check if we need one per material on renderer, idk if this is only first index
        _shadowMaterials = new Material[materialCount];
        for (int i = 0; i < materialCount; i++) _shadowMaterials[i] = shadowMaterial;
    }
    
    public bool Process()
    {
        // some people animate renderer.enabled instead of gameObject.activeInHierarchy
        // do not disable shadow clone game object, it causes a flicker when re-enabled!
        bool shouldRender = _mainMesh.enabled && _mainMesh.gameObject.activeInHierarchy;
        
        // GraphicsBuffer becomes stale when mesh is disabled
        if (!shouldRender)
        {
            _frameInitCounter = 0;
            _hasInitialized = false;
            _shadowMesh.forceRenderingOff = true; // force off if mesh is disabled
            return false; // TODO: dispose stale buffers
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
        
        _frameInitCounter++;
        return false;
    }

    public void RenderForShadow()
    {
        ResetShadowClone();
        RenderShadowClone();
    }
    
    public void RenderForUiCulling()
    {
        ConfigureShadowCloneForUiCulling();
        RenderShadowClone();
    }
    
    public void Dispose()
    {
        if (_shadowMesh != null)
        {
            // Cleanup instanced Mesh & Materials
            GameObject shadowMeshObject = _shadowMesh.gameObject;
            UnityEngine.Object.Destroy(_shadowMeshFilter.mesh);
            UnityEngine.Object.Destroy(_shadowMeshFilter);
            
            // explain why this works
            if (_hasShadowMaterials) _shadowMesh.sharedMaterials = _mainMesh.sharedMaterials;
            foreach (Material mat in _shadowMesh.sharedMaterials) UnityEngine.Object.Destroy(mat);
            
            UnityEngine.Object.Destroy(_shadowMesh);
            UnityEngine.Object.Destroy(shadowMeshObject);
        }
        
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
        _targetBuffer?.Dispose();
        _targetBuffer = null;
        _computeBuffer?.Dispose();
        _computeBuffer = null;
    }
    
    #endregion
    
    #region Private Methods
    
    // Unity is weird, so we need to wait 2 frames before we can get the graphics buffer
    private void SetupGraphicsBuffer()
    {
        Mesh mesh = _mainMesh.sharedMesh;
        Mesh shadowMesh = _shadowMesh.GetComponent<MeshFilter>().mesh;
        
        _bufferLayout = 0;
        if (mesh.HasVertexAttribute(VertexAttribute.Position)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Normal)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Tangent)) _bufferLayout += 4;
        _bufferLayout *= 4; // 4 bytes per float
        
        const float xThreadGroups = 32f;
        _threadGroups = Mathf.CeilToInt(mesh.vertexCount / xThreadGroups);
        
        _mainMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        shadowMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        _targetBuffer = shadowMesh.GetVertexBuffer(0);
        
        //Debug.Log($"Initialized! BufferLayout: {_bufferLayout}, GraphicsBuffer: {_graphicsBuffer != null}, TargetBuffer: {_targetBuffer != null}");
    }

    public static void FindExclusionVertList(SkinnedMeshRenderer renderer, FPRExclusion exclusion) 
    {
        var boneWeights = renderer.sharedMesh.boneWeights;
        
        HashSet<int> weights = new();
        for (int i = 0; i < renderer.bones.Length; i++)
        {
            if (exclusion.affectedChildren.Contains(renderer.bones[i]))
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

            exclusion.affectedVertexIndices.Add(bone != null ? i : -1);
        }
    }

    public void ResetMainMesh()
    {
        _mainMesh.shadowCastingMode = ShadowCastingMode.On;
        _mainMesh.forceRenderingOff = false;

        _shadowMesh.transform.position = Vector3.positiveInfinity; // nan 
    }
    
    private void ResetShadowClone()
    {
        if (ShadowCloneManager.s_DebugShowShadow)
        {
            _mainMesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            _mainMesh.forceRenderingOff = false;
            
            _shadowMesh.shadowCastingMode = ShadowCastingMode.On;
            _shadowMesh.forceRenderingOff = false;
            
            _shadowMesh.transform.localPosition = Vector3.zero;
        }
        else
        {
            _mainMesh.shadowCastingMode = ShadowCastingMode.On;
            _mainMesh.forceRenderingOff = false;
            
            _shadowMesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            _shadowMesh.forceRenderingOff = !_shouldCastShadows;
        }
        
        //_shadowMesh.enabled = true;
        
        // shadow casting needs clone to have original materials (uv discard)
        // we also want to respect material swaps... but this is fucking slow :(

        if (!ShadowCloneManager.s_CopyMaterialsToShadow)
            return;

        _shadowMesh.sharedMaterials = _mainMesh.sharedMaterials;
        UpdateCloneMaterialProperties();
    }

    private void ConfigureShadowCloneForUiCulling()
    {
        _shadowMesh.shadowCastingMode = ShadowCastingMode.On;
        _shadowMesh.forceRenderingOff = false;
        
        // UI culling needs clone to have write-to-depth shader
        _shadowMesh.sharedMaterials = _shadowMaterials;
        
        // Not needed- MaterialPropertyBlock applied to renderer in RenderForShadow
        UpdateCloneMaterialProperties();
    }
    
    private void RenderShadowClone()
    {
        // thanks sdraw, i suck at matrix math
        Matrix4x4 rootMatrix = _mainMesh.localToWorldMatrix.inverse * Matrix4x4.TRS(_rootBone.position, _rootBone.rotation, Vector3.one);
        
        _graphicsBuffer = _mainMesh.GetVertexBuffer();
        ShadowCloneHelper.shader.SetMatrix(s_SourceRootMatrix, rootMatrix);
        ShadowCloneHelper.shader.SetBuffer(0, s_SourceBufferId, _graphicsBuffer);
        ShadowCloneHelper.shader.SetBuffer(0, s_TargetBufferId, _targetBuffer);
        
        ShadowCloneHelper.shader.SetBuffer(0, s_HiddenVerticiesId, _computeBuffer);
        ShadowCloneHelper.shader.SetVector(s_HiddenVertexPos, Vector4.positiveInfinity); // temp
        
        ShadowCloneHelper.shader.SetInt(s_SourceBufferLayoutId, _bufferLayout);
        ShadowCloneHelper.shader.Dispatch(0, _threadGroups, 1, 1);
        _graphicsBuffer.Release();
    }
    
    private void UpdateCloneMaterialProperties()
    {
        // copy material properties to shadow clone materials
        _mainMesh.GetPropertyBlock(_shadowMaterialBlock);
        _shadowMesh.SetPropertyBlock(_shadowMaterialBlock);
    }

    #endregion
}