using UnityEngine;

namespace NAK.CleanPlates.Helpers;

// Holder for the per-avatar scaling info.
public class NameplateAnchor : MonoBehaviour
{
    public Transform HeadBone;
    public float LocalTopFromRoot;
    public float LocalTopAboveHead;

    private Transform _transform;
    private float _rootHeight;
    private float _headHeight;
    private bool _hasHeadBone;

    private void Awake()
    {
        _transform = transform;
        _hasHeadBone = HeadBone;
        UpdateCachedHeightsOnScaleChange();
    }

    public void UpdateCachedHeightsOnScaleChange()
    {
        float scaleY = _transform.lossyScale.y;
        _rootHeight = LocalTopFromRoot * scaleY;
        _headHeight = LocalTopAboveHead * scaleY;
    }

    public Vector3 GetRootAnchorPosition(float padding = 0f)
        => _transform.position + _transform.up * (_rootHeight + padding);

    public Vector3 GetHeadAnchorPosition(float padding = 0f)
        => _hasHeadBone
            ? HeadBone.position + _transform.up * (_headHeight + padding)
            : _transform.position + _transform.up * (_rootHeight + padding);
}