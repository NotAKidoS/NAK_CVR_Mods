using UnityEngine;

namespace NAK.PropSpawnTweaks.Components;

public class PropLoadingHexagon : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _hexRenderer;
    [SerializeField] private TMPro.TextMeshPro _loadingText;
    [SerializeField] private Transform[] _hexTransforms;
    private float _scale;
    
    private void Update()
    {
        if (_scale < 1f)
        {
            _scale += Time.deltaTime * 4f;
            transform.GetChild(0).localScale = Vector3.one * _scale;
        }

        for (int i = 0; i < 3; i++)
        {
            // give slightly different rotation to each hexagon
            _hexTransforms[i].Rotate(Vector3.up, 30f * Time.deltaTime * (i + 1));
        }
    }
    
    public void SetLoadingText(string text)
        => _loadingText.text = text; // SetAllDirty

    public void SetLoadingShape(float value)
        => _hexRenderer.SetBlendShapeWeight(0, value);
}