using ABI_RC.Core.Player.ShadowClone;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Initialization
    
    private void InitializeCollections()
    {
        _standardRenderers = new List<MeshRenderer>();
        _standardFilters = new List<MeshFilter>();
        _skinnedRenderers = new List<SkinnedMeshRenderer>();
        _allSourceRenderers = new List<Renderer>();
        
        _standardClones = new List<MeshRenderer>();
        _standardCloneFilters = new List<MeshFilter>();
        _skinnedClones = new List<SkinnedMeshRenderer>();
        
        _standardRenderersNeedingChecks = new List<int>();
        _skinnedRenderersNeedingChecks = new List<int>();
        _cachedSkinnedBoneCounts = new List<int>();
        _cachedSharedMeshes = new List<Mesh>();
        
        _localMaterials = new List<Material[]>();
        _cullingMaterials = new List<Material[]>();
        _mainMaterials = new List<Material>();
        _propertyBlock = new MaterialPropertyBlock();
        
        _blendShapeWeights = new List<List<float>>();
    }
    
    private void InitializeRenderers()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        
        // Pre-size lists based on found renderers
        // _standardRenderers.Capacity = renderers.Length;
        // _standardFilters.Capacity = renderers.Length;
        // _skinnedRenderers.Capacity = renderers.Length;
        // _allSourceRenderers.Capacity = renderers.Length;

        // Sort renderers into their respective lists
        foreach (Renderer render in renderers)
        {
            _allSourceRenderers.Add(render);
            
            switch (render)
            {
                case MeshRenderer meshRenderer:
                {
                    MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        _standardRenderers.Add(meshRenderer);
                        _standardFilters.Add(filter);
                    }
                    break;
                }
                case SkinnedMeshRenderer skinnedRenderer:
                {
                    if (skinnedRenderer.sharedMesh != null) _skinnedRenderers.Add(skinnedRenderer);
                    break;
                }
            }
        }
    }
    
    private void SetupMaterialsAndBlendShapes()
    {
        // Cache counts
        int standardCount = _standardRenderers.Count;
        int skinnedCount = _skinnedRenderers.Count;
        var standardRenderers = _standardRenderers;
        var skinnedRenderers = _skinnedRenderers;
        var localMats = _localMaterials;
        var cullingMats = _cullingMaterials;
        var blendWeights = _blendShapeWeights;
        
        // Setup standard renderer materials
        for (int i = 0; i < standardCount; i++)
        {
            MeshRenderer render = standardRenderers[i];
            int matCount = render.sharedMaterials.Length;
            
            // Local materials array
            var localMatArray = new Material[matCount];
            for (int j = 0; j < matCount; j++) localMatArray[j] = render.sharedMaterials[j];
            localMats.Add(localMatArray);
            
            // Culling materials array
            var cullingMatArray = new Material[matCount];
            for (int j = 0; j < matCount; j++) cullingMatArray[j] = ShadowCloneUtils.cullingMaterial;
            cullingMats.Add(cullingMatArray);
        }
        
        // Setup skinned renderer materials and blend shapes
        for (int i = 0; i < skinnedCount; i++)
        {
            SkinnedMeshRenderer render = skinnedRenderers[i];
            int matCount = render.sharedMaterials.Length;
            
            // Local materials array
            var localMatArray = new Material[matCount];
            for (int j = 0; j < matCount; j++) localMatArray[j] = render.sharedMaterials[j];
            localMats.Add(localMatArray);
            
            // Culling materials array
            var cullingMatArray = new Material[matCount];
            for (int j = 0; j < matCount; j++) cullingMatArray[j] = ShadowCloneUtils.cullingMaterial;
            cullingMats.Add(cullingMatArray);
            
            // Blend shape weights
            int blendShapeCount = render.sharedMesh.blendShapeCount;
            var weights = new List<float>(blendShapeCount);
            for (int j = 0; j < blendShapeCount; j++) weights.Add(0f);
            blendWeights.Add(weights);
        }
        
        // Initialize renderer state arrays
        int totalRenderers = _allSourceRenderers.Count;
        _originallyHadShadows = new bool[totalRenderers];
        _originallyWasEnabled = new bool[totalRenderers];
    }

    #endregion Initialization
}