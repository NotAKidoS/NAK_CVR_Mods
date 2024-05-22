using ABI.CCK.Components;
using UnityEngine;

namespace NAK.RelativeSync.Components;

public class RelativeSyncMarker : MonoBehaviour
{
    public int pathHash { get; private set; }

    public bool ApplyRelativePosition = true;
    public bool ApplyRelativeRotation = true;
    public bool OnlyApplyRelativeHeading;
    
    private void Start()
    {
        string path = GetGameObjectPath(transform);
        pathHash = path.GetHashCode();
        RelativeSyncManager.RelativeSyncTransforms.Add(pathHash, this);

        ConfigureForPotentialMovementParent();
    }

    private void OnDestroy()
    {
        RelativeSyncManager.RelativeSyncTransforms.Remove(pathHash);
    }
    
    private void ConfigureForPotentialMovementParent()
    {
        if (!gameObject.TryGetComponent(out CVRMovementParent movementParent)) 
            return;
        
        // TODO: a refactor may be needed to handle the orientation mode being animated
        
        // respect orientation mode & gravity zone
        ApplyRelativeRotation = movementParent.orientationMode == CVRMovementParent.OrientationMode.RotateWithParent;
        OnlyApplyRelativeHeading = movementParent.GetComponent<GravityZone>() == null;
    }
    
    private static string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}