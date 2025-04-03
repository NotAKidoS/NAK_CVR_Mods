using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region State Syncing
    
    private void SyncEnabledState()
    {
#if ENABLE_PROFILER
        s_CopyEnabledState.Begin();
#endif
        
        int currentIndex = 0;
        
        // Update skinned mesh renderers
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++, currentIndex++)
        {
            SkinnedMeshRenderer source = _skinnedRenderers[i];
            _skinnedClones[i].enabled = _rendererActiveStates[currentIndex] = IsRendererActive(source);
        }
        
        // Update mesh renderers
        int meshCount = _meshRenderers.Count;
        for (int i = 0; i < meshCount; i++, currentIndex++)
        {
            MeshRenderer source = _meshRenderers[i];
            if (Setting_CloneMeshRenderers) _meshClones[i].enabled = _rendererActiveStates[currentIndex] = IsRendererActive(source);
            else _rendererActiveStates[currentIndex] = IsRendererActive(source);
        }
        
        // Update other renderers
        int otherCount = _otherRenderers.Count;
        for (int i = 0; i < otherCount; i++, currentIndex++)
        {
            Renderer source = _otherRenderers[i];
            _rendererActiveStates[currentIndex] = IsRendererActive(source);
        }
        
#if ENABLE_PROFILER
        s_CopyEnabledState.End();
#endif
    }    
    
    private void SyncMaterials()
    {
#if ENABLE_PROFILER
        s_CopyMaterials.Begin();
#endif  
        int currentIndex = 0;
        
        // Sync skinned mesh materials
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++, currentIndex++)
        {
            if (!_rendererActiveStates[currentIndex])
                continue;
            
            CopyMaterialsAndProperties(
                _skinnedRenderers[i],
                _skinnedClones[i],
                _propertyBlock,
                _materialWorkingList,
                _skinnedCloneMaterials[i]);
        }
        
        // Sync mesh materials if enabled
        if (Setting_CloneMeshRenderers)
        {
            int meshCount = _meshRenderers.Count;
            for (int i = 0; i < meshCount; i++, currentIndex++)
            {
                if (!_rendererActiveStates[currentIndex]) 
                    continue;
                
                CopyMaterialsAndProperties(
                    _meshRenderers[i],
                    _meshClones[i],
                    _propertyBlock,
                    _materialWorkingList,
                    _meshCloneMaterials[i]);
            }
        }
        
#if ENABLE_PROFILER
        s_CopyMaterials.End();
#endif
    }

    private void SyncBlendShapes()
    {
#if ENABLE_PROFILER
        s_CopyBlendShapes.Begin();
#endif
        
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++)
        {
            SkinnedMeshRenderer source = _skinnedRenderers[i];
            if (!_rendererActiveStates[i]) 
                continue;
            
            CopyBlendShapes(
                source,
                _skinnedClones[i],
                _blendShapeWeights[i]);
        }
        
#if ENABLE_PROFILER
        s_CopyBlendShapes.End();
#endif
    }

    private static void CopyMaterialsAndProperties(
        Renderer source,
        Renderer clone,
        MaterialPropertyBlock propertyBlock,
        List<Material> workingList,
        Material[] cloneMaterials)
    {
        source.GetSharedMaterials(workingList);
        
        int matCount = workingList.Count;
        bool hasChanged = false;
        
        for (int i = 0; i < matCount; i++)
        {
            if (ReferenceEquals(workingList[i], cloneMaterials[i])) continue;
            cloneMaterials[i] = workingList[i];
            hasChanged = true;
        }
        if (hasChanged) clone.sharedMaterials = cloneMaterials;
        
        source.GetPropertyBlock(propertyBlock);
        clone.SetPropertyBlock(propertyBlock);
    }

    private static void CopyBlendShapes(
        SkinnedMeshRenderer source,
        SkinnedMeshRenderer clone,
        List<float> weights)
    {
        int weightCount = weights.Count;
        for (int i = 0; i < weightCount; i++)
        {
            float weight = source.GetBlendShapeWeight(i);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (weight == weights[i]) continue; // Halves the work
            clone.SetBlendShapeWeight(i, weights[i] = weight);
        }
    }
    
    #endregion State Syncing
}