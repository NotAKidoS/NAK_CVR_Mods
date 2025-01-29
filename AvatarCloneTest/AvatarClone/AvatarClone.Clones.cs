using ABI_RC.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Clone Creation
    
    private void CreateClones()
    {
        int standardCount = _standardRenderers.Count;
        _standardClones = new List<MeshRenderer>(standardCount);
        _standardCloneFilters = new List<MeshFilter>(standardCount);
        for (int i = 0; i < standardCount; i++) CreateStandardClone(i);

        int skinnedCount = _skinnedRenderers.Count;
        _skinnedClones = new List<SkinnedMeshRenderer>(skinnedCount);
        for (int i = 0; i < skinnedCount; i++) CreateSkinnedClone(i);
    }

    private void CreateStandardClone(int index)
    {
        MeshRenderer sourceRenderer = _standardRenderers[index];
        MeshFilter sourceFilter = _standardFilters[index];
        
        GameObject go = new(sourceRenderer.name + "_VisualClone")
        {
            layer = CVRLayers.PlayerClone
        };
        
        go.transform.SetParent(sourceRenderer.transform, false);
        //go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        MeshRenderer cloneRenderer = go.AddComponent<MeshRenderer>();
        MeshFilter cloneFilter = go.AddComponent<MeshFilter>();
        
        // Initial setup
        cloneRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        cloneRenderer.probeAnchor = sourceRenderer.probeAnchor;
        cloneRenderer.localBounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);
        cloneFilter.sharedMesh = sourceFilter.sharedMesh;
        
        // Optimizations to enforce
        cloneRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        cloneRenderer.allowOcclusionWhenDynamic = false;
        
        // Optimizations to enforce
        sourceRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        sourceRenderer.allowOcclusionWhenDynamic = false;

        _standardClones.Add(cloneRenderer);
        _standardCloneFilters.Add(cloneFilter);
    }

    private void CreateSkinnedClone(int index)
    {
        SkinnedMeshRenderer source = _skinnedRenderers[index];
        
        GameObject go = new(source.name + "_VisualClone")
        {
            layer = CVRLayers.PlayerClone
        };
        
        go.transform.SetParent(source.transform, false);
        //go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        SkinnedMeshRenderer clone = go.AddComponent<SkinnedMeshRenderer>();
        
        // Initial setup
        clone.sharedMaterials = source.sharedMaterials;
        clone.shadowCastingMode = ShadowCastingMode.Off;
        clone.probeAnchor = source.probeAnchor;
        clone.localBounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);
        clone.sharedMesh = source.sharedMesh;
        clone.rootBone = source.rootBone;
        clone.bones = source.bones;
        clone.quality = source.quality;
        clone.updateWhenOffscreen = source.updateWhenOffscreen;

        // Optimizations to enforce
        clone.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        clone.allowOcclusionWhenDynamic = false;
        clone.updateWhenOffscreen = false;
        clone.skinnedMotionVectors = false;
        clone.forceMatrixRecalculationPerRender = false;
        clone.quality = SkinQuality.Bone4;
        
        // Optimizations to enforce
        source.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        source.allowOcclusionWhenDynamic = false;
        source.updateWhenOffscreen = false;
        source.skinnedMotionVectors = false;
        source.forceMatrixRecalculationPerRender = false;
        source.quality = SkinQuality.Bone4;
        
        _skinnedClones.Add(clone);
    }
    
    #endregion Clone Creation
}