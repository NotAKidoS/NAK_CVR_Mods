using UnityEngine;

namespace NAK.BetterShadowClone;

/// <summary>
/// Manual exclusion component for the TransformHider (FPR) system.
/// Allows you to manually hide and show a transform that would otherwise be hidden.
/// </summary>
public class FPRExclusion : MonoBehaviour
{
    public Transform target;

    internal readonly List<Transform> affectedChildren = new();
    internal readonly List<IFPRExclusionTask> relatedTasks = new();

    private void OnEnable()
        => SetFPRState(true);

    private void OnDisable()
        => SetFPRState(false);
    
    private void SetFPRState(bool state)
    {
        if (relatedTasks == null) return; // no hiders to set
        foreach (IFPRExclusionTask task in relatedTasks)
            task.IsActive = state;
    }
}

public interface IFPRExclusionTask
{
    public bool IsActive { get; set; }
}