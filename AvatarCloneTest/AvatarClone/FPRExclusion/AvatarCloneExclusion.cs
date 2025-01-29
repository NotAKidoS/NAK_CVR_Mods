using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public class AvatarCloneExclusion : IExclusionBehaviour
{
    private readonly AvatarClone _cloneSystem;
    private readonly Transform _target;
    private Transform _shrinkBone;
    
    public bool isImmuneToGlobalState { get; set; }

    public AvatarCloneExclusion(AvatarClone cloneSystem, Transform target)
    {
        _cloneSystem = cloneSystem;
        _target = target;
    }

    public void UpdateExclusions(bool isShown, bool shrinkToZero)
    {
        Debug.Log($"[AvatarClone2] Updating exclusion for {_target.name}: isShown={isShown}, shrinkToZero={shrinkToZero}");
        
        if (_shrinkBone == null)
        {
            // Create shrink bone parented directly to target
            _shrinkBone = new GameObject($"{_target.name}_Shrink").transform;
            _shrinkBone.SetParent(_target, false);
            Debug.Log($"[AvatarClone2] Created shrink bone for {_target.name}");
        }
        
        // Set scale based on shrink mode
        _shrinkBone.localScale = shrinkToZero ? Vector3.zero : Vector3.positiveInfinity;
        
        // Let the clone system handle the update
        _cloneSystem.HandleExclusionUpdate(_target, _shrinkBone, isShown);
    }
}