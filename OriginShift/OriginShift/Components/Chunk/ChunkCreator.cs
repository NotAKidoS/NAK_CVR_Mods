using UnityEngine;

namespace NAK.OriginShift.Components;

public class ChunkCreator : MonoBehaviour
{
    [SerializeField] private GameObject _chunkPrefab;
    
    private bool _isChunkCreated;
    
    public void CreateChunk()
    {
        _isChunkCreated = true;
        
        Transform transform1 = transform;
        Instantiate(_chunkPrefab, transform1.position, Quaternion.identity, transform1);
    }
    
    public void DestroyChunk()
    {
        if (!_isChunkCreated)
            return;
        
        _isChunkCreated = false;
        Destroy(gameObject.transform.GetChild(0).gameObject);
    }
}