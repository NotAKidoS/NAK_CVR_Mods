using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    private readonly Dictionary<Transform, HashSet<Renderer>> _exclusionDirectRenderers = new();
    private readonly Dictionary<Transform, HashSet<Transform>> _exclusionControlledBones = new();
    
    private void InitializeExclusions()
    {
        // Add head exclusion for humanoid avatars if not present
        var animator = GetComponent<Animator>();
        if (animator != null && animator.isHuman)
        {
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone != null && headBone.GetComponent<FPRExclusion>() == null)
            {
                var exclusion = headBone.gameObject.AddComponent<FPRExclusion>();
                exclusion.isShown = false;
                exclusion.target = headBone;
                exclusion.shrinkToZero = true;
            }
        }

        // Process existing exclusions bottom-up
        var exclusions = GetComponentsInChildren<FPRExclusion>(true);
        
        for (int i = exclusions.Length - 1; i >= 0; i--)
        {
            var exclusion = exclusions[i];
            if (exclusion.target == null)
                exclusion.target = exclusion.transform;
                
            // Skip invalid exclusions or already processed targets
            if (exclusion.target == null || _exclusionDirectRenderers.ContainsKey(exclusion.target))
            {
                Destroy(exclusion);
                continue;
            }
            
            // Initialize data for this exclusion
            _exclusionDirectRenderers[exclusion.target] = new HashSet<Renderer>();
            _exclusionControlledBones[exclusion.target] = new HashSet<Transform>();
            
            // Set up our behaviour
            exclusion.behaviour = new AvatarCloneExclusion(this, exclusion.target);
            
            // Collect affected renderers and bones
            CollectExclusionData(exclusion.target);
            
            // Initial update
            exclusion.UpdateExclusions();
        }
    }
    
    private void CollectExclusionData(Transform target)
    {
        var stack = new Stack<Transform>();
        stack.Push(target);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            
            // Skip if this transform belongs to another exclusion
            if (current != target && current.GetComponent<FPRExclusion>() != null)
                continue;
                
            _exclusionControlledBones[target].Add(current);
            
            // Add renderers that will need their clone visibility toggled
            foreach (var renderer in current.GetComponents<Renderer>())
            {
                // Find corresponding clone renderer
                if (renderer is MeshRenderer meshRenderer)
                {
                    int index = _standardRenderers.IndexOf(meshRenderer);
                    if (index != -1)
                        _exclusionDirectRenderers[target].Add(_standardClones[index]);
                }
                else if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    int index = _skinnedRenderers.IndexOf(skinnedRenderer);
                    if (index != -1)
                        _exclusionDirectRenderers[target].Add(_skinnedClones[index]);
                }
            }
            
            // Add children to stack
            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }
    }
    
    public void HandleExclusionUpdate(Transform target, Transform shrinkBone, bool isShown)
    {
        if (!_exclusionDirectRenderers.TryGetValue(target, out var directCloneRenderers) ||
            !_exclusionControlledBones.TryGetValue(target, out var controlledBones))
            return;

        // Handle direct clone renderers
        foreach (var cloneRenderer in directCloneRenderers)
        {
            cloneRenderer.enabled = isShown;
        }

        // Update bone references in clone renderers
        int cloneCount = _skinnedClones.Count;
        var cloneRenderers = _skinnedClones;
        var sourceRenderers = _skinnedRenderers;
        
        for (int i = 0; i < cloneCount; i++)
        {
            var clone = cloneRenderers[i];
            var source = sourceRenderers[i];
            var sourceBones = source.bones;
            var cloneBones = clone.bones;
            int boneCount = cloneBones.Length;
            bool needsUpdate = false;
            
            for (int j = 0; j < boneCount; j++)
            {
                // Check if this bone is in our controlled set
                if (controlledBones.Contains(sourceBones[j]))
                {
                    cloneBones[j] = isShown ? sourceBones[j] : shrinkBone;
                    needsUpdate = true;
                }
            }
            
            if (needsUpdate)
            {
                clone.bones = cloneBones;
            }
        }
    }
}