using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public struct MeshShadowClone : IShadowClone
{
    // We technically don't need a clone mesh for MeshRenderer shadow clone handling,
    // but as the shadows are also utilized for UI culling, we need to have a clone mesh.
    // If we don't stick with UI culling, we can just set the shadowCastingMode to ShadowsOnly when player camera renders.
    
    // lame 2 frame init stuff
    private const int FrameInitCount = 0;
    private int _frameInitCounter;
    private bool _hasInitialized;
    
    // shadow is used to cull ui, clone always exists
    private readonly bool _shouldCastShadows;
    private readonly MeshRenderer _mainMesh;
    private readonly MeshRenderer _shadowMesh;
    private readonly MeshFilter _shadowMeshFilter;

    // material copying (unity is shit)
    private bool _hasShadowMaterials;
    private readonly Material[] _shadowMaterials;
    private readonly MaterialPropertyBlock _shadowMaterialBlock;
    
    #region IShadowClone Methods
    
    public void ResetMainMesh(){}
    
    public bool IsValid => _mainMesh != null && _shadowMesh != null;
    
    public MeshShadowClone(MeshRenderer meshRenderer)
    {
        _mainMesh = meshRenderer;
        MeshFilter _mainMeshFilter = meshRenderer.GetComponent<MeshFilter>();
        
        if (_mainMesh == null
            || _mainMesh.sharedMaterials == null
            || _mainMesh.sharedMaterials.Length == 0
            || _mainMeshFilter == null
            || _mainMeshFilter.sharedMesh == null)
        {
            Dispose();
            return; // no mesh!
        }
        
        _shouldCastShadows = _mainMesh.shadowCastingMode != ShadowCastingMode.Off;
        _mainMesh.shadowCastingMode = ShadowCastingMode.Off; // visual mesh doesn't cast shadows

        (_shadowMesh, _shadowMeshFilter) = ShadowCloneManager.InstantiateShadowClone(_mainMesh);
        _shadowMesh.forceRenderingOff = true;
        
        // material copying shit
        int materialCount = _mainMesh.sharedMaterials.Length;
        Material shadowMaterial = ShadowCloneHelper.shadowMaterial;
        
        _shadowMaterialBlock = new MaterialPropertyBlock();
        _shadowMaterials = new Material[materialCount];
        for (int i = 0; i < materialCount; i++) _shadowMaterials[i] = shadowMaterial;
    }
    
    public bool Process()
    {
        bool shouldRender = _mainMesh.enabled && _mainMesh.gameObject.activeInHierarchy;
        
        // copying behaviour of SkinnedShadowClone, to visually be the same when a mesh toggles
        if (!shouldRender)
        {
            _frameInitCounter = 0;
            _hasInitialized = false;
            _shadowMesh.forceRenderingOff = true;
            return false;
        }
        
        if (_frameInitCounter >= FrameInitCount)
        {
            if (_hasInitialized) 
                return true;
            
            _hasInitialized = true;
            return true;
        }
        
        _frameInitCounter++;
        return false;
    }

    public void RenderForShadow()
    {
        _shadowMesh.shadowCastingMode = ShadowCloneManager.s_DebugShowShadow 
            ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
        
        _shadowMesh.forceRenderingOff = !_shouldCastShadows;
        
        // shadow casting needs clone to have original materials (uv discard)
        // we also want to respect material swaps... but this is fucking slow :(

        if (!ShadowCloneManager.s_CopyMaterialsToShadow)
            return;

        if (_hasShadowMaterials)
        {
            // NOTE: will not handle material swaps unless Avatar Overrender Ui is on
            _shadowMesh.sharedMaterials = _mainMesh.sharedMaterials;
            _hasShadowMaterials = false;
        }
        
        UpdateCloneMaterialProperties();
    }

    public void RenderForUiCulling()
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
    
    public void Dispose()
    {
        if (_shadowMesh == null) 
            return; // uh oh
        
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
    
    #endregion

    #region Private Methods
    
    private void UpdateCloneMaterialProperties()
    {
        // copy material properties to shadow clone materials
        _mainMesh.GetPropertyBlock(_shadowMaterialBlock);
        _shadowMesh.SetPropertyBlock(_shadowMaterialBlock);
    }
    
    #endregion
}