using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class SkinnedShadowClone : IShadowClone
{
    private static readonly int s_SourceBufferId = Shader.PropertyToID("_sourceBuffer");
    private static readonly int s_TargetBufferId = Shader.PropertyToID("_targetBuffer");
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
    private int _threadGroups;
    private int _bufferLayout;
    
    // material copying (unity is shit)
    private bool _hasShadowMaterials;
    private readonly Material[] _shadowMaterials;
    private readonly MaterialPropertyBlock _shadowMaterialBlock;

    #region IShadowClone Methods
    
    // anything player can touch is suspect to death
    public bool IsValid => _mainMesh != null && _shadowMesh != null && _rootBone != null;
    
    internal SkinnedShadowClone(SkinnedMeshRenderer renderer)
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
        
        _shouldCastShadows = _mainMesh.shadowCastingMode != ShadowCastingMode.Off;
        _mainMesh.shadowCastingMode = ShadowCastingMode.Off; // visual mesh doesn't cast shadows
        
        (_shadowMesh, _shadowMeshFilter) = ShadowCloneManager.InstantiateShadowClone(_mainMesh);
        _shadowMesh.forceRenderingOff = true;
        
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
            UnityEngine.Object.Destroy(_shadowMeshFilter.sharedMesh);
            UnityEngine.Object.Destroy(_shadowMeshFilter);

            if (!_hasShadowMaterials)
            {
                var materials = _shadowMesh.sharedMaterials;
                foreach (Material mat in materials) UnityEngine.Object.Destroy(mat);
            }
            
            UnityEngine.Object.Destroy(_shadowMesh);
            UnityEngine.Object.Destroy(shadowMeshObject);
        }
        
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
        _targetBuffer?.Dispose();
        _targetBuffer = null;
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
        
        const float xThreadGroups = 64f;
        _threadGroups = Mathf.CeilToInt(mesh.vertexCount / xThreadGroups);
        
        _mainMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        shadowMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        _targetBuffer = shadowMesh.GetVertexBuffer(0);
        
        //Debug.Log($"Initialized! BufferLayout: {_bufferLayout}, GraphicsBuffer: {_graphicsBuffer != null}, TargetBuffer: {_targetBuffer != null}");
    }

    private void ResetShadowClone()
    {
        _shadowMesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        _shadowMesh.forceRenderingOff = !_shouldCastShadows;
        
        // shadow casting needs clone to have original materials (uv discard)
        // we also want to respect material swaps... but this is fucking slow :(

        if (!ShadowCloneManager.s_CopyMaterialsToShadow)
            return;

        if (_hasShadowMaterials)
        {
            _shadowMesh.sharedMaterials = _mainMesh.sharedMaterials;
            _hasShadowMaterials = false;
        }
        
        UpdateCloneMaterialProperties();
    }

    private void ConfigureShadowCloneForUiCulling()
    {
        _shadowMesh.shadowCastingMode = ShadowCastingMode.On;
        _shadowMesh.forceRenderingOff = false;
        
        // UI culling needs clone to have write-to-depth shader
        if (_hasShadowMaterials) return;
        _shadowMesh.sharedMaterials = _shadowMaterials;
        _hasShadowMaterials = true;
        
        // Not needed- MaterialPropertyBlock applied to renderer in RenderForShadow
        //UpdateCloneMaterialProperties();
    }
    
    private void RenderShadowClone()
    {
        // thanks sdraw, i suck at matrix math
        Matrix4x4 rootMatrix = _mainMesh.localToWorldMatrix.inverse * Matrix4x4.TRS(_rootBone.position, _rootBone.rotation, Vector3.one);
        
        _graphicsBuffer = _mainMesh.GetVertexBuffer();
        ShadowCloneHelper.shader.SetMatrix(s_SourceRootMatrix, rootMatrix);
        ShadowCloneHelper.shader.SetBuffer(0, s_SourceBufferId, _graphicsBuffer);
        ShadowCloneHelper.shader.SetBuffer(0, s_TargetBufferId, _targetBuffer);
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