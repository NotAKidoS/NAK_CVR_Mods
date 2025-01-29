using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Update Methods

    private void UpdateStandardRenderers()
    {
        int count = _standardRenderers.Count;
        var sourceRenderers = _standardRenderers;
        var cloneRenderers = _standardClones;
        var localMats = _localMaterials;
        
        for (int i = 0; i < count; i++)
        {
            if (!IsRendererValid(sourceRenderers[i])) continue;
            CopyMaterialsAndProperties(
                sourceRenderers[i],
                cloneRenderers[i],
                _propertyBlock,
                _mainMaterials,
                localMats[i]);
        }
    }

    private void UpdateSkinnedRenderers()
    {
        int standardCount = _standardRenderers.Count;
        int count = _skinnedRenderers.Count;
        var sourceRenderers = _skinnedRenderers;
        var cloneRenderers = _skinnedClones;
        var localMats = _localMaterials;
        var blendWeights = _blendShapeWeights;
        
        for (int i = 0; i < count; i++)
        {
            SkinnedMeshRenderer source = sourceRenderers[i];
            if (!IsRendererValid(source)) continue;
            
            SkinnedMeshRenderer clone = cloneRenderers[i];
            CopyMaterialsAndProperties(
                source,
                clone,
                _propertyBlock,
                _mainMaterials,
                localMats[i + standardCount]);
            
            CopyBlendShapes(source, clone, blendWeights[i]);
        }
    }

    private void UpdateStandardRenderersWithChecks()
    {
        s_CopyMeshes.Begin();
        
        var cloneFilters = _standardCloneFilters;
        var sourceFilters = _standardFilters;
        var cachedMeshes = _cachedSharedMeshes;
        var checkIndices = _standardRenderersNeedingChecks;
        int checkCount = checkIndices.Count;
        
        while (cachedMeshes.Count < checkCount) cachedMeshes.Add(null);
        
        for (int i = 0; i < checkCount; i++)
        {
            int rendererIndex = checkIndices[i];
            Mesh newMesh = sourceFilters[rendererIndex].sharedMesh;
            if (ReferenceEquals(newMesh, cachedMeshes[i])) continue;
            cloneFilters[rendererIndex].sharedMesh = newMesh; // expensive & allocates
            cachedMeshes[i] = newMesh;
        }
        
        s_CopyMeshes.End();
    }

    private void UpdateSkinnedRenderersWithChecks()
    {
        s_CopyMeshes.Begin();
        
        var sourceRenderers = _skinnedRenderers;
        var cloneRenderers = _skinnedClones;
        var cachedMeshes = _cachedSharedMeshes;
        var cachedBoneCounts = _cachedSkinnedBoneCounts;
        var checkIndices = _skinnedRenderersNeedingChecks;
        int checkCount = checkIndices.Count;
        int meshOffset = _standardRenderersNeedingChecks.Count;

        // Ensure cache lists are properly sized
        while (cachedMeshes.Count < meshOffset + checkCount) cachedMeshes.Add(null);
        while (cachedBoneCounts.Count < checkCount) cachedBoneCounts.Add(0);
        
        for (int i = 0; i < checkCount; i++)
        {
            int rendererIndex = checkIndices[i];
            SkinnedMeshRenderer source = sourceRenderers[rendererIndex];
            SkinnedMeshRenderer clone = cloneRenderers[rendererIndex];
            
            // Check mesh changes
            Mesh newMesh = source.sharedMesh; // expensive & allocates
            if (!ReferenceEquals(newMesh, cachedMeshes[meshOffset + i]))
            {
                clone.sharedMesh = newMesh;
                cachedMeshes[meshOffset + i] = newMesh;
            }
            
            // Check bone changes
            var sourceBones = source.bones;
            int newBoneCount = sourceBones.Length;
            int oldBoneCount = cachedBoneCounts[i];
            if (newBoneCount == oldBoneCount) 
                continue;
            
            var cloneBones = clone.bones; // expensive & allocates
            if (newBoneCount > oldBoneCount)
            {
                // Resize array and copy only the new bones (Magica Cloth appends bones when enabling Mesh Cloth)
                Array.Resize(ref cloneBones, newBoneCount);
                for (int boneIndex = oldBoneCount; boneIndex < newBoneCount; boneIndex++)
                    cloneBones[boneIndex] = sourceBones[boneIndex];
                clone.bones = cloneBones;
            }
            else
            {
                // If shrinking, just set the whole array
                clone.bones = sourceBones;
            }
            
            cachedBoneCounts[i] = newBoneCount;
        }
        
        s_CopyMeshes.End();
    }
    
    private static void CopyMaterialsAndProperties(
        Renderer source, Renderer clone,
        MaterialPropertyBlock propertyBlock, 
        List<Material> mainMaterials, 
        Material[] localMaterials)
    {
        s_CopyMaterials.Begin();

        source.GetSharedMaterials(mainMaterials);

        int matCount = mainMaterials.Count;
        bool hasChanged = false;
        for (var i = 0; i < matCount; i++)
        {
            if (ReferenceEquals(mainMaterials[i], localMaterials[i])) continue;
            localMaterials[i] = mainMaterials[i];
            hasChanged = true;
        }
        if (hasChanged) clone.sharedMaterials = localMaterials;
        
        source.GetPropertyBlock(propertyBlock);
        clone.SetPropertyBlock(propertyBlock);
        
        s_CopyMaterials.End();
    }

    private static void CopyBlendShapes(
        SkinnedMeshRenderer source, 
        SkinnedMeshRenderer target, 
        List<float> blendShapeWeights)
    {
        s_CopyBlendShapes.Begin();
        
        int weightCount = blendShapeWeights.Count;
        for (var i = 0; i < weightCount; i++)
        {
            var weight = source.GetBlendShapeWeight(i);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (weight == blendShapeWeights[i]) continue; // Halves the work
            target.SetBlendShapeWeight(i, blendShapeWeights[i] = weight);
        }
        
        s_CopyBlendShapes.End();
    }
    #endregion Update Methods
}