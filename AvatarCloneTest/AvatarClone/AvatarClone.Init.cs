using ABI_RC.Core.Player.ShadowClone;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Initialization
    
    private void InitializeCollections()
    {
#if ENABLE_PROFILER
        s_InitializeData.Begin();
#endif
        
        // Initialize source collections
        _skinnedRenderers = new List<SkinnedMeshRenderer>();
        _blendShapeWeights = new List<List<float>>();
        
        _meshRenderers = new List<MeshRenderer>();
        _meshFilters = new List<MeshFilter>();
        
        _otherRenderers = new List<Renderer>();
        
        // Initialize clone collections
        _skinnedClones = new List<SkinnedMeshRenderer>();
        _skinnedCloneMaterials = new List<Material[]>();
        _skinnedCloneCullingMaterials = new List<Material[]>();
        
        if (Setting_CloneMeshRenderers)
        {
            _meshClones = new List<MeshRenderer>();
            _meshCloneFilters = new List<MeshFilter>();
            _meshCloneMaterials = new List<Material[]>();
            _meshCloneCullingMaterials = new List<Material[]>();
        }
        
        // Initialize shared resources
        _materialWorkingList = new List<Material>();
        _propertyBlock = new MaterialPropertyBlock();
        
#if ENABLE_PROFILER
        s_InitializeData.End();
#endif
    }
    
    private void CollectRenderers()
    {
    #if ENABLE_PROFILER
        s_InitializeData.Begin();
    #endif
        
        var renderers = GetComponentsInChildren<Renderer>(true);
        var currentIndex = 0;
        var nonCloned = 0;
        
        // Single pass: directly categorize renderers
        foreach (Renderer renderer in renderers)
        {
            switch (renderer)
            {
                case SkinnedMeshRenderer skinned when skinned.sharedMesh != null:
                    AddSkinnedRenderer(skinned);
                    currentIndex++;
                    break;
                    
                case MeshRenderer mesh:
                    MeshFilter filter = mesh.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        if (Setting_CloneMeshRenderers)
                        {
                            AddMeshRenderer(mesh, filter);
                        }
                        else
                        {
                            AddMeshRenderer(mesh, filter);
                            nonCloned++;
                        }
                        currentIndex++;
                    }
                    break;
                    
                default:
                    AddOtherRenderer(renderer);
                    currentIndex++;
                    nonCloned++;
                    break;
            }
        }
        
        _rendererActiveStates = new bool[currentIndex];
        _originalShadowCastingMode = new ShadowCastingMode[currentIndex];
        _sourceShouldBeHiddenFromFPR = new bool[nonCloned];

    #if ENABLE_PROFILER
        s_InitializeData.End();
    #endif
    }

    private void AddSkinnedRenderer(SkinnedMeshRenderer renderer)
    {
#if ENABLE_PROFILER
        s_AddRenderer.Begin();
#endif
        
        _skinnedRenderers.Add(renderer);
        
        // Clone materials array for clone renderer
        var materials = renderer.sharedMaterials;
        var cloneMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++) cloneMaterials[i] = materials[i];
        _skinnedCloneMaterials.Add(cloneMaterials);

        // Cache culling materials
        var cullingMaterialArray = new Material[materials.Length];
#if !UNITY_EDITOR
        for (int i = 0; i < materials.Length; i++) cullingMaterialArray[i] = ShadowCloneUtils.cullingMaterial;
#else
        for (int i = 0; i < materials.Length; i++) cullingMaterialArray[i] = cullingMaterial;
#endif
        _skinnedCloneCullingMaterials.Add(cullingMaterialArray);
        
        // Cache blend shape weights
        var weights = new List<float>(renderer.sharedMesh.blendShapeCount);
        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++) weights.Add(0f);
        _blendShapeWeights.Add(weights);
        
#if ENABLE_PROFILER
        s_AddRenderer.End();
#endif
    }
    
    private void AddMeshRenderer(MeshRenderer renderer, MeshFilter filter)
    {
#if ENABLE_PROFILER
        s_AddRenderer.Begin();
#endif
        
        _meshRenderers.Add(renderer);
        _meshFilters.Add(filter);
        
        if (!Setting_CloneMeshRenderers) return;
        
        // Clone materials array for clone renderer
        var materials = renderer.sharedMaterials;
        var cloneMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++) cloneMaterials[i] = materials[i];
        _meshCloneMaterials.Add(cloneMaterials);

        // Cache culling materials
        var cullingMaterialArray = new Material[materials.Length];
#if !UNITY_EDITOR
        for (int i = 0; i < materials.Length; i++) cullingMaterialArray[i] = ShadowCloneUtils.cullingMaterial;
#else
        for (int i = 0; i < materials.Length; i++) cullingMaterialArray[i] = cullingMaterial;
#endif
        _meshCloneCullingMaterials.Add(cullingMaterialArray);
        
#if ENABLE_PROFILER
        s_AddRenderer.End();
#endif
    }
    
    private void AddOtherRenderer(Renderer renderer)
    {
#if ENABLE_PROFILER
        s_AddRenderer.Begin();
#endif        
        _otherRenderers.Add(renderer);
#if ENABLE_PROFILER
        s_AddRenderer.End();
#endif
    }

    private void CreateClones()
    {
#if ENABLE_PROFILER
        s_InitializeData.Begin();
#endif

        // Always create skinned mesh clones
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++)
        {
            CreateSkinnedClone(i);
        }
        
        // Optionally create mesh clones
        if (Setting_CloneMeshRenderers)
        {
            int meshCount = _meshRenderers.Count;
            for (int i = 0; i < meshCount; i++)
            {
                CreateMeshClone(i);
            }
        }
        
#if ENABLE_PROFILER
        s_InitializeData.End();
#endif
    }

    private void CreateSkinnedClone(int index)
    {
#if ENABLE_PROFILER
        s_CreateClone.Begin();
#endif
        
        SkinnedMeshRenderer source = _skinnedRenderers[index];
        
        GameObject clone = new(source.name + "_Clone")
        {
            layer = CLONE_LAYER
        };
        
        clone.transform.SetParent(source.transform, false);
        
        SkinnedMeshRenderer cloneRenderer = clone.AddComponent<SkinnedMeshRenderer>();
        
        // Basic setup
        cloneRenderer.sharedMaterials = _skinnedCloneMaterials[index];
        cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        cloneRenderer.probeAnchor = source.probeAnchor;
        cloneRenderer.sharedMesh = source.sharedMesh;
        cloneRenderer.rootBone = source.rootBone;
        cloneRenderer.bones = source.bones;
        
#if !UNITY_EDITOR
        cloneRenderer.localBounds = new Bounds(source.localBounds.center, source.localBounds.size * 2f);
#endif
        
        // Quality settings
        cloneRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        cloneRenderer.allowOcclusionWhenDynamic = false;
        cloneRenderer.updateWhenOffscreen = false;
        cloneRenderer.skinnedMotionVectors = false;
        cloneRenderer.forceMatrixRecalculationPerRender = false;
        cloneRenderer.quality = SkinQuality.Bone4;
        
        source.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        source.allowOcclusionWhenDynamic = false;
        source.updateWhenOffscreen = false;
        source.skinnedMotionVectors = false;
        source.forceMatrixRecalculationPerRender = false;
        source.quality = SkinQuality.Bone4;
        
        // Add to clone list
        _skinnedClones.Add(cloneRenderer);
        
#if ENABLE_PROFILER
        s_CreateClone.End();
#endif
    }

    private void CreateMeshClone(int index)
    {
#if ENABLE_PROFILER
        s_CreateClone.Begin();
#endif
        
        MeshRenderer source = _meshRenderers[index];
        MeshFilter sourceFilter = _meshFilters[index];
        
        GameObject clone = new(source.name + "_Clone")
        {
            layer = CLONE_LAYER
        };
        
        clone.transform.SetParent(source.transform, false);
        
        MeshRenderer cloneRenderer = clone.AddComponent<MeshRenderer>();
        MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();
        
        // Basic setup
        cloneRenderer.sharedMaterials = _meshCloneMaterials[index];
        cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        cloneRenderer.probeAnchor = source.probeAnchor;
        
#if !UNITY_EDITOR
        cloneRenderer.localBounds = new Bounds(source.localBounds.center, source.localBounds.size * 2f);
#endif
        
        cloneFilter.sharedMesh = sourceFilter.sharedMesh;
        
        // Quality settings
        cloneRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        cloneRenderer.allowOcclusionWhenDynamic = false;

        source.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        source.allowOcclusionWhenDynamic = false;
        
        // Add to clone lists
        _meshClones.Add(cloneRenderer);
        _meshCloneFilters.Add(cloneFilter);
        
#if ENABLE_PROFILER
        s_CreateClone.End();
#endif
    }
    
    #endregion Initialization
}