using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Exclusions
    
    private FPRExclusion[] _exclusions;

    private void AddExclusionToHeadIfNeeded()
    {
        if (!TryGetComponent(out Animator animator)
            || !animator.isHuman
            || !animator.avatar
            || !animator.avatar.isValid) 
            return;
        
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (!head) 
            return;
        
        GameObject headGo = head.gameObject;
        if (headGo.TryGetComponent(out FPRExclusion exclusion))
            return;
        
        exclusion = headGo.AddComponent<FPRExclusion>();
        exclusion.target = head;
        exclusion.isShown = false;
    }
    
    private void InitializeExclusions()
    {
        _exclusions = GetComponentsInChildren<FPRExclusion>(true);
        var exclusionRoots = new Dictionary<Transform, AvatarCloneExclusion>(_exclusions.Length);

        // **1. Precompute Exclusions**
        foreach (FPRExclusion exclusion in _exclusions)
        {
            Transform target = exclusion.target ??= exclusion.transform;
            if (exclusionRoots.ContainsKey(target) || !target.gameObject.scene.IsValid()) 
                continue;

            AvatarCloneExclusion behaviour = new AvatarCloneExclusion(this, target);
            exclusion.behaviour = behaviour;
            exclusionRoots.Add(target, behaviour);
        }

        // Process Exclusion Transforms
        Renderer ourRenderer;

        void ProcessTransformHierarchy(Transform current, Transform root, AvatarCloneExclusion behaviour)
        {
            if (exclusionRoots.ContainsKey(current) && current != root) return;

            behaviour.affectedTransforms.Add(current);
            if (current.TryGetComponent(out ourRenderer))
                behaviour.affectedRenderers.Add(ourRenderer);

            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                if (!exclusionRoots.ContainsKey(child))
                    ProcessTransformHierarchy(child, root, behaviour);
            }
        }

        foreach (var entry in exclusionRoots)
        {
            Transform rootTransform = entry.Key;
            AvatarCloneExclusion behaviour = entry.Value;
            ProcessTransformHierarchy(rootTransform, rootTransform, behaviour);
            behaviour.affectedTransformSet = new HashSet<Transform>(behaviour.affectedTransforms);
        }

        // ------------------------------
        // **OPTIMIZED EXCLUSION BONE MAPPING**
        // ------------------------------

        Dictionary<Transform, AvatarCloneExclusion>.ValueCollection exclusionBehaviours = exclusionRoots.Values;
        int skinnedCount = _skinnedClones.Count;

        // **2. Precompute Bone-to-Exclusion Mapping**
        int estimatedBoneCount = skinnedCount * 20; // Estimated bones per skinned mesh
        var boneToExclusion = new Dictionary<Transform, List<AvatarCloneExclusion>>(estimatedBoneCount);

        foreach (AvatarCloneExclusion behaviour in exclusionBehaviours)
        {
            foreach (Transform bone in behaviour.affectedTransformSet)
            {
                if (!boneToExclusion.TryGetValue(bone, out var list))
                {
                    list = new List<AvatarCloneExclusion>(2);
                    boneToExclusion[bone] = list;
                }
                list.Add(behaviour);
            }
        }

        // **3. Process Skinned Mesh Renderers**
        for (int s = 0; s < skinnedCount; s++)
        {
            SkinnedMeshRenderer source = _skinnedRenderers[s];
            var bones = source.bones; // Cache bones array
            
            SkinnedMeshRenderer smr = _skinnedClones[s];
            int boneCount = bones.Length;

            for (int i = 0; i < boneCount; i++)
            {
                Transform bone = bones[i];

                // **Skip if the bone isn't mapped to exclusions**
                if (!bone // Skip null bones
                    || !boneToExclusion.TryGetValue(bone, out var behaviours))
                    continue;

                // **Avoid redundant dictionary lookups**
                for (int j = 0; j < behaviours.Count; j++)
                {
                    AvatarCloneExclusion behaviour = behaviours[j];

                    if (!behaviour.skinnedToBoneIndex.TryGetValue(smr, out var indices))
                    {
                        indices = new List<int>(4);
                        behaviour.skinnedToBoneIndex[smr] = indices;
                    }
                    
                    indices.Add(i);
                }
            }
        }
        
        ApplyInitialExclusionState();
    }
    
    public void ApplyInitialExclusionState()
    {
        foreach (FPRExclusion exclusion in _exclusions)
        {
            exclusion._wasShown = exclusion.isShown;
            if (!exclusion.isShown) exclusion.UpdateExclusions();
        }
    }

    public void HandleExclusionUpdate(AvatarCloneExclusion exclusion, bool isShown)
    {
#if ENABLE_PROFILER
        s_UpdateExclusions.Begin();
#endif

        // **1. Update Renderer Visibility**
        foreach (Renderer renderer in exclusion.affectedRenderers)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                int index = _skinnedRenderers.IndexOf(skinned);
                if (index >= 0) _skinnedClones[index].gameObject.SetActive(isShown);
            }
            else if (renderer is MeshRenderer mesh)
            {
                int index = _meshRenderers.IndexOf(mesh);
                if (index >= 0)
                {
                    if (Setting_CloneMeshRenderers)
                    {
                        _meshClones[index].gameObject.SetActive(isShown);
                    }
                    else
                    {
                        // Other renderer (never cloned) - update shadow casting state
                        _sourceShouldBeHiddenFromFPR[index] = !isShown; // When hidden, use for shadows
                    }
                }
            }
            else if (renderer)
            {
                int index = _otherRenderers.IndexOf(renderer);
                if (index >= 0)
                {
                    int shadowIndex = index + (Setting_CloneMeshRenderers ? _meshRenderers.Count : 0);
                    _sourceShouldBeHiddenFromFPR[shadowIndex] = !isShown; // When hidden, use for shadows
                }
            }
        }

        // **2. Update Bone References in Skinned Mesh Renderers**
        UpdateSkinnedMeshBones(exclusion, exclusion._shrinkBone, isShown);
    
#if ENABLE_PROFILER
        s_UpdateExclusions.End();
#endif
    }

    private void UpdateSkinnedMeshBones(AvatarCloneExclusion exclusion, Transform shrinkBone, bool isShown)
    {
#if ENABLE_PROFILER
        s_HandleBoneUpdates.Begin();
#endif

        foreach (var smrEntry in exclusion.skinnedToBoneIndex)
        {
            SkinnedMeshRenderer smr = smrEntry.Key;
            var indices = smrEntry.Value;
            bool needsUpdate = false;
            
            var parentBones = smr.transform.parent.GetComponent<SkinnedMeshRenderer>().bones;
            var cloneBones = smr.bones;
            Array.Resize(ref cloneBones, parentBones.Length);
            
            // Only modify our bones, other exclusions may have modified others
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (!isShown) cloneBones[index] = shrinkBone;
                else cloneBones[index] = parentBones[index];
                needsUpdate = true;
            }
            if (needsUpdate) smr.bones = cloneBones;
        }

#if ENABLE_PROFILER
        s_HandleBoneUpdates.End();
#endif
    }
    
    #endregion Exclusions
}