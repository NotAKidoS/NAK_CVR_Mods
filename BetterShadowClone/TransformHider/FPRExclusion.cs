using UnityEngine;

namespace NAK.BetterShadowClone;

/// <summary>
/// Manual exclusion component for the TransformHider (FPR) system.
/// Allows you to manually hide and show a transform that would otherwise be hidden.
/// </summary>
public class FPRExclusion : MonoBehaviour
{
    public Transform target;

    internal List<Transform> affectedChildren = new();
    
    [NonSerialized]
    internal ITransformHider[] relevantHiders;

    private void OnEnable()
        => SetFPRState(true);

    private void OnDisable()
        => SetFPRState(false);
    
    private void SetFPRState(bool state)
    {
        if (relevantHiders == null) return; // no hiders to set
        foreach (ITransformHider hider in relevantHiders)
            hider.IsActive = state;
    }
}