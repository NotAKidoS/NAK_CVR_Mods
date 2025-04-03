using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public class AvatarCloneExclusion : IExclusionBehaviour
{
    public readonly Dictionary<SkinnedMeshRenderer, List<int>> skinnedToBoneIndex = new();
    public readonly List<Transform> affectedTransforms = new();
    public readonly List<Renderer> affectedRenderers = new();
    public HashSet<Transform> affectedTransformSet = new();
    
    private readonly AvatarClone _cloneSystem;
    private readonly Transform _target;
    internal Transform _shrinkBone;
    
    public bool isImmuneToGlobalState { get; set; }

    public AvatarCloneExclusion(AvatarClone cloneSystem, Transform target)
    {
        _cloneSystem = cloneSystem;
        _target = target;
    }

    public void UpdateExclusions(bool isShown, bool shrinkToZero)
    {
        if (_shrinkBone == null)
        {
            // Create shrink bone parented directly to target
            _shrinkBone = new GameObject($"{_target.name}_Shrink").transform;
            _shrinkBone.SetParent(_target, false);
        }
        // Set scale based on shrink mode
        _shrinkBone.localScale = shrinkToZero ? Vector3.zero : Vector3.positiveInfinity;
        
        // Replace the bone references with the shrink bone for the indicies we modify
        _cloneSystem.HandleExclusionUpdate(this, isShown);
    }
}